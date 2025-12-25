using EcommerceAPI.Business.Abstract;
using EcommerceAPI.Entities.DTOs;
using EcommerceAPI.Core.Utilities.Results;
using EcommerceAPI.DataAccess.Abstract;
using EcommerceAPI.Entities.Concrete;
using EcommerceAPI.Entities.Enums;
using EcommerceAPI.Core.Interfaces;
using EcommerceAPI.Core.CrossCuttingConcerns.Logging;
using Microsoft.EntityFrameworkCore;
using EcommerceAPI.Business.Extensions;

namespace EcommerceAPI.Business.Concrete;

public class OrderManager : IOrderService
{
    private readonly IOrderDal _orderDal;
    private readonly IProductDal _productDal;
    private readonly IInventoryService _inventoryService;
    private readonly ICartService _cartService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICouponService _couponService;
    private readonly IAuditService _auditService;

    private const decimal FreeShippingThreshold = 1000m;
    private const decimal ShippingCost = 29.90m;

    public OrderManager(
        IOrderDal orderDal,
        IProductDal productDal,
        IInventoryService inventoryService,
        ICartService cartService,
        IUnitOfWork unitOfWork,
        ICouponService couponService,
        IAuditService auditService)
    {
        _orderDal = orderDal;
        _productDal = productDal;
        _inventoryService = inventoryService;
        _cartService = cartService;
        _unitOfWork = unitOfWork;
        _couponService = couponService;
        _auditService = auditService;
    }

    public async Task<IDataResult<OrderDto>> CheckoutAsync(int userId, CheckoutRequest request)
    {
        var cartResult = await ValidateCartAndStockAsync(userId);
        if (!cartResult.Success) return new ErrorDataResult<OrderDto>(cartResult.Message);
        var cartDto = cartResult.Data;

        var subtotal = cartDto.TotalAmount;
        var shippingCost = CalculateShippingCost(subtotal);
        
        var couponResult = await ValidateCouponAsync(request.CouponCode, subtotal);
        if (!couponResult.Success) return new ErrorDataResult<OrderDto>(couponResult.Message);
        var couponData = couponResult.Data;

        var order = CreateOrderEntity(userId, request, cartDto, subtotal, shippingCost, couponData);
        
        await _orderDal.AddAsync(order);

        try
        {
            await ProcessStockAndCouponUsageAsync(order, cartDto, userId, couponData.Id);
            await _unitOfWork.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            return new ErrorDataResult<OrderDto>($"Sipariş oluşturulamadı: {ex.Message}");
        }

        var createdOrder = await _orderDal.GetByIdWithDetailsAsync(order.Id);
        
        await _auditService.LogActionAsync(
            userId.ToString(),
            "CreateOrder",
            "Order",
            new { OrderId = order.Id, OrderNumber = order.OrderNumber, TotalAmount = order.TotalAmount, ItemCount = order.OrderItems.Count });
        
        return new SuccessDataResult<OrderDto>(createdOrder!.ToDto(), "Sipariş başarıyla oluşturuldu.");
    }

    public async Task<IDataResult<OrderDto>> GetOrderAsync(int userId, int orderId)
    {
        var order = await _orderDal.GetByIdWithDetailsAsync(orderId);

        if (order == null || order.UserId != userId)
            return new ErrorDataResult<OrderDto>("Sipariş bulunamadı.");

        return new SuccessDataResult<OrderDto>(order.ToDto());
    }

    public async Task<IDataResult<List<OrderDto>>> GetUserOrdersAsync(int userId)
    {
        var orders = await _orderDal.GetUserOrdersAsync(userId);
        return new SuccessDataResult<List<OrderDto>>(orders.Select(x => x.ToDto()).ToList());
    }

    public async Task<IDataResult<OrderDto>> CancelOrderAsync(int userId, int orderId, string? status = null)
    {
        if (!string.IsNullOrEmpty(status) && status != "Cancelled")
        {
            return new ErrorDataResult<OrderDto>("Only 'Cancelled' status is supported via this endpoint logic currently.");
        }

        var order = await _orderDal.GetByIdWithDetailsAsync(orderId);

        if (order == null || order.UserId != userId)
             return new ErrorDataResult<OrderDto>("Sipariş bulunamadı.");

        if (order.Status != OrderStatus.PendingPayment)
             return new ErrorDataResult<OrderDto>("Sadece ödeme bekleyen siparişler iptal edilebilir.");

        order.Status = OrderStatus.Cancelled;
        order.CancelledAt = DateTime.UtcNow;

        foreach (var item in order.OrderItems)
        {
            await _inventoryService.IncreaseStockAsync(
                item.ProductId, 
                item.Quantity, 
                userId, 
                $"Sipariş İptali - Sipariş No: {order.OrderNumber}");
        }

        _orderDal.Update(order);
        await _unitOfWork.SaveChangesAsync();

        await _auditService.LogActionAsync(
            userId.ToString(),
            "CancelOrder",
            "Order",
            new { OrderId = order.Id, OrderNumber = order.OrderNumber });

        return new SuccessDataResult<OrderDto>(order.ToDto(), "Sipariş iptal edildi.");
    }

    public async Task<IResult> CancelExpiredOrdersAsync()
    {
        var expiryTime = DateTime.UtcNow.AddMinutes(-30);
        var expiredOrders = await _orderDal.GetExpiredPendingOrdersAsync(expiryTime);

        if (!expiredOrders.Any()) return new SuccessResult();

        foreach (var order in expiredOrders)
        {
            order.Status = OrderStatus.Cancelled;
            order.Notes = (order.Notes ?? "") + " | [Sistem] Ödeme zaman aşımı nedeniyle iptal edildi.";
            order.CancelledAt = DateTime.UtcNow;

            foreach (var item in order.OrderItems)
            {
                await _inventoryService.IncreaseStockAsync(
                    item.ProductId,
                    item.Quantity,
                    order.UserId,
                    $"Sistem İptali - Sipariş No: {order.OrderNumber}");
            }

            _orderDal.Update(order);
        }

        await _unitOfWork.SaveChangesAsync();
        return new SuccessResult($"{expiredOrders.Count} adet zaman aşımına uğrayan sipariş iptal edildi.");
    }

    public async Task<IDataResult<List<OrderDto>>> GetAllOrdersAsync()
    {
        var orders = await _orderDal.GetAllOrdersWithDetailsAsync();
        return new SuccessDataResult<List<OrderDto>>(orders.Select(x => x.ToDto()).ToList());
    }

    public async Task<IDataResult<OrderDto>> UpdateOrderStatusAsync(int orderId, string status)
    {
        var order = await _orderDal.GetByIdWithDetailsAsync(orderId);
        if (order == null) return new ErrorDataResult<OrderDto>("Sipariş bulunamadı.");

        if (!Enum.TryParse<OrderStatus>(status, true, out var orderStatus))
        {
            return new ErrorDataResult<OrderDto>($"Geçersiz sipariş durumu: {status}");
        }

        var previousStatus = order.Status;
        order.Status = orderStatus;
        _orderDal.Update(order);
        await _unitOfWork.SaveChangesAsync();

        await _auditService.LogActionAsync(
            "System",
            "UpdateOrderStatus",
            "Order",
            new { OrderId = order.Id, OrderNumber = order.OrderNumber, PreviousStatus = previousStatus.ToString(), NewStatus = orderStatus.ToString() });

        return new SuccessDataResult<OrderDto>(order.ToDto(), "Sipariş durumu güncellendi.");
    }

    public async Task<IDataResult<OrderDto>> UpdateOrderItemsAsync(int userId, int orderId, UpdateOrderItemsRequest request)
    {
        var order = await _orderDal.GetByIdWithDetailsAsync(orderId);
        
        var validationResult = ValidateOrderForUpdate(order, userId);
        if (!validationResult.Success) return new ErrorDataResult<OrderDto>(validationResult.Message);

        if (request.Items == null || !request.Items.Any())
            return new ErrorDataResult<OrderDto>("Sipariş en az bir ürün içermelidir.");

        var products = await LoadProductsForUpdateAsync(request.Items);
        if (products.Count != request.Items.Select(i => i.ProductId).Distinct().Count())
            return new ErrorDataResult<OrderDto>("Bazı ürünler bulunamadı.");

        var existingItems = order.OrderItems.ToDictionary(oi => oi.ProductId, oi => oi);
        var newItems = request.Items.ToDictionary(i => i.ProductId, i => i);

        var (itemsToRemove, itemsToAdd, itemsToUpdate) = IdentifyChanges(existingItems, newItems);

        var stockValidation = ValidateStockAvailability(products, newItems, itemsToAdd, itemsToUpdate, existingItems);
        if (!stockValidation.Success) return new ErrorDataResult<OrderDto>(stockValidation.Message);

        try
        {
            await ProcessOrderUpdatesAsync(order, userId, itemsToRemove, itemsToAdd, itemsToUpdate, newItems, existingItems, products);
            RecalculateOrderTotals(order);
            
            _orderDal.Update(order);
            await _unitOfWork.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            return new ErrorDataResult<OrderDto>($"Sipariş güncellenemedi: {ex.Message}");
        }

        var updatedOrder = await _orderDal.GetByIdWithDetailsAsync(order.Id);
        
        await _auditService.LogActionAsync(
            userId.ToString(),
            "UpdateOrderItems",
            "Order",
            new { OrderId = order.Id, OrderNumber = order.OrderNumber, NewTotalAmount = order.TotalAmount });
        
        return new SuccessDataResult<OrderDto>(updatedOrder!.ToDto(), "Sipariş güncellendi.");
    }



    private IResult ValidateOrderForUpdate(Order? order, int userId)
    {
        if (order == null || order.UserId != userId)
            return new ErrorResult("Sipariş bulunamadı.");

        if (order.Status != OrderStatus.PendingPayment)
            return new ErrorResult("Sadece ödeme bekleyen siparişler düzenlenebilir.");

        return new SuccessResult();
    }

    private async Task<Dictionary<int, Product>> LoadProductsForUpdateAsync(List<UpdateOrderItemDto> items)
    {
        var products = new Dictionary<int, Product>();
        foreach (var item in items)
        {
            var product = await _productDal.GetWithInventoryAsync(item.ProductId);
            if (product != null) products[item.ProductId] = product;
        }
        return products;
    }

    private (List<int> Removed, List<int> Added, List<int> Updated) IdentifyChanges(
        Dictionary<int, OrderItem> existingItems,
        Dictionary<int, UpdateOrderItemDto> newItems)
    {
        var removed = existingItems.Keys.Except(newItems.Keys).ToList();
        var added = newItems.Keys.Except(existingItems.Keys).ToList();
        var updated = existingItems.Keys.Intersect(newItems.Keys).ToList();
        return (removed, added, updated);
    }

    private IResult ValidateStockAvailability(
        Dictionary<int, Product> products,
        Dictionary<int, UpdateOrderItemDto> newItems,
        List<int> addedIds,
        List<int> updatedIds,
        Dictionary<int, OrderItem> existingItems)
    {
        foreach (var productId in addedIds)
        {
            var requestedQty = newItems[productId].Quantity;
            var availableStock = products[productId].Inventory?.QuantityAvailable ?? 0;
            if (requestedQty > availableStock)
                return new ErrorResult($"Stok yetersiz: {products[productId].Name}");
        }

        foreach (var productId in updatedIds)
        {
            var existingQty = existingItems[productId].Quantity;
            var requestedQty = newItems[productId].Quantity;
            var qtyDiff = requestedQty - existingQty;
            
            if (qtyDiff > 0)
            {
                var availableStock = products[productId].Inventory?.QuantityAvailable ?? 0;
                if (qtyDiff > availableStock)
                    return new ErrorResult($"Stok yetersiz: {products[productId].Name}");
            }
        }
        return new SuccessResult();
    }

    private async Task ProcessOrderUpdatesAsync(
        Order order,
        int userId,
        List<int> itemsToRemove,
        List<int> itemsToAdd,
        List<int> itemsToUpdate,
        Dictionary<int, UpdateOrderItemDto> newItems,
        Dictionary<int, OrderItem> existingItems,
        Dictionary<int, Product> products)
    {

        foreach (var productId in itemsToRemove)
        {
            var item = existingItems[productId];
            await _inventoryService.IncreaseStockAsync(
                productId, item.Quantity, userId,
                $"Sipariş Düzenleme (Ürün Çıkarıldı) - Sipariş No: {order.OrderNumber}");
            order.OrderItems.Remove(item);
        }


        foreach (var productId in itemsToAdd)
        {
            var qty = newItems[productId].Quantity;
            var product = products[productId];
            
            var stockResult = await _inventoryService.DecreaseStockAsync(
                productId, qty, userId,
                $"Sipariş Düzenleme (Ürün Eklendi) - Sipariş No: {order.OrderNumber}");
            
            if (!stockResult.Success) throw new Exception(stockResult.Message);

            order.OrderItems.Add(new OrderItem
            {
                ProductId = productId,
                Quantity = qty,
                PriceSnapshot = product.Price
            });
        }


        foreach (var productId in itemsToUpdate)
        {
            var existingQty = existingItems[productId].Quantity;
            var requestedQty = newItems[productId].Quantity;
            var qtyDiff = requestedQty - existingQty;

            if (qtyDiff > 0)
            {
                var stockResult = await _inventoryService.DecreaseStockAsync(
                    productId, qtyDiff, userId,
                    $"Sipariş Düzenleme (Miktar Artırıldı) - Sipariş No: {order.OrderNumber}");
                if (!stockResult.Success) throw new Exception(stockResult.Message);
            }
            else if (qtyDiff < 0)
            {
                await _inventoryService.IncreaseStockAsync(
                    productId, Math.Abs(qtyDiff), userId,
                    $"Sipariş Düzenleme (Miktar Azaltıldı) - Sipariş No: {order.OrderNumber}");
            }

            existingItems[productId].Quantity = requestedQty;
        }
    }

    private void RecalculateOrderTotals(Order order)
    {
        decimal subtotal = order.OrderItems.Sum(oi => oi.PriceSnapshot * oi.Quantity);
        decimal shippingCost = subtotal >= FreeShippingThreshold ? 0 : ShippingCost;
        order.TotalAmount = subtotal - order.DiscountAmount + shippingCost;

        if (order.Payment != null)
        {
            order.Payment.Amount = order.TotalAmount;
        }
    }

    private static string GenerateOrderNumber()
    {
        return $"ORD-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..8].ToUpper()}";
    }



    private async Task<IDataResult<CartDto>> ValidateCartAndStockAsync(int userId)
    {
        var cartResult = await _cartService.GetCartAsync(userId);
        if (!cartResult.Success) return new ErrorDataResult<CartDto>(cartResult.Message);

        var cartDto = cartResult.Data;
        if (cartDto == null || !cartDto.Items.Any())
            return new ErrorDataResult<CartDto>("Sepetiniz boş. Sipariş oluşturmak için sepete ürün ekleyin.");

        foreach (var item in cartDto.Items)
        {
            if (item.Quantity > item.AvailableStock)
                return new ErrorDataResult<CartDto>($"Stok yetersiz: {item.ProductName}");
        }

        return new SuccessDataResult<CartDto>(cartDto);
    }

    private decimal CalculateShippingCost(decimal subtotal)
    {
        return subtotal >= FreeShippingThreshold ? 0 : ShippingCost;
    }

    private async Task<IDataResult<(int? Id, string? Code, decimal Amount)>> ValidateCouponAsync(string? couponCode, decimal subtotal)
    {
        if (string.IsNullOrEmpty(couponCode))
            return new SuccessDataResult<(int? Id, string? Code, decimal Amount)>((null, null, 0));

        var couponResult = await _couponService.ValidateCouponAsync(couponCode, subtotal);
        if (!couponResult.Success)
            return new ErrorDataResult<(int? Id, string? Code, decimal Amount)>("Kupon doğrulanamadı.");
        
        var validation = couponResult.Data!;
        if (!validation.IsValid)
            return new ErrorDataResult<(int? Id, string? Code, decimal Amount)>(validation.ErrorMessage ?? "Geçersiz kupon kodu.");
        
        return new SuccessDataResult<(int? Id, string? Code, decimal Amount)>((validation.Coupon!.Id, validation.Coupon.Code, validation.DiscountAmount));
    }

    private Order CreateOrderEntity(int userId, CheckoutRequest request, CartDto cartDto, decimal subtotal, decimal shippingCost, (int? Id, string? Code, decimal Amount) couponData)
    {
        var order = new Order
        {
            UserId = userId,
            OrderNumber = GenerateOrderNumber(),
            Status = OrderStatus.PendingPayment,
            ShippingAddress = request.ShippingAddress,
            Notes = request.Notes ?? string.Empty,
            TotalAmount = subtotal - couponData.Amount + shippingCost,
            CouponId = couponData.Id,
            CouponCode = couponData.Code,
            DiscountAmount = couponData.Amount
        };

        foreach (var cartItem in cartDto.Items)
        {
            order.OrderItems.Add(new OrderItem
            {
                ProductId = cartItem.ProductId,
                Quantity = cartItem.Quantity,
                PriceSnapshot = cartItem.UnitPrice 
            });
        }

        order.Payment = new Payment
        {
            Amount = order.TotalAmount,
            Status = PaymentStatus.Pending,
            PaymentMethod = request.PaymentMethod,
            IdempotencyKey = Guid.NewGuid().ToString()
        };

        return order;
    }

    private async Task ProcessStockAndCouponUsageAsync(Order order, CartDto cartDto, int userId, int? couponId)
    {
        foreach (var cartItem in cartDto.Items)
        {
            var stockResult = await _inventoryService.DecreaseStockAsync(
                cartItem.ProductId, 
                cartItem.Quantity, 
                userId, 
                $"Satış - Sipariş No: {order.OrderNumber}");
            
            if (!stockResult.Success)
                 throw new Exception(stockResult.Message);
        }

        if (couponId.HasValue)
        {
            var usageResult = await _couponService.IncrementUsageAsync(couponId.Value);
            if (!usageResult.Success)
                throw new Exception("Kupon kullanımı kaydedilemedi.");
        }
    }
}


