using EcommerceAPI.Business.Abstract;
using EcommerceAPI.Entities.DTOs;
using EcommerceAPI.Core.Utilities.Results;
using EcommerceAPI.DataAccess.Abstract;
using EcommerceAPI.Entities.Concrete;
using EcommerceAPI.Entities.Enums;
using EcommerceAPI.Core.Interfaces;
using EcommerceAPI.Core.CrossCuttingConcerns.Logging;
using EcommerceAPI.Business.Extensions;
using Microsoft.Extensions.Logging;

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
    private readonly ILogger<OrderManager> _logger;

    private const decimal FreeShippingThreshold = 1000m;
    private const decimal ShippingCost = 29.90m;
    private const int OrderTimeoutMinutes = 30;

    public OrderManager(
        IOrderDal orderDal,
        IProductDal productDal,
        IInventoryService inventoryService,
        ICartService cartService,
        IUnitOfWork unitOfWork,
        ICouponService couponService,
        IAuditService auditService,
        ILogger<OrderManager> logger)
    {
        _orderDal = orderDal;
        _productDal = productDal;
        _inventoryService = inventoryService;
        _cartService = cartService;
        _unitOfWork = unitOfWork;
        _couponService = couponService;
        _auditService = auditService;
        _logger = logger;
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


        var productQuantities = cartDto.Items.ToDictionary(i => i.ProductId, i => i.Quantity);
        var order = CreateOrderEntity(userId, request, cartDto, subtotal, shippingCost, couponData);

        await _unitOfWork.BeginTransactionAsync();
        try
        {

            var stockReservation = await _inventoryService.ReserveStocksAsync(productQuantities, userId, "Order Reservation: " + order.OrderNumber);
            if (!stockReservation.Success)
            {

                await _unitOfWork.RollbackTransactionAsync();
                return new ErrorDataResult<OrderDto>($"Stok rezervasyon hatası: {stockReservation.Message}");
            }


            await _orderDal.AddAsync(order);

            await ProcessCouponUsageAsync(order, couponData.Id);
            
            await _cartService.ClearCartAsync(userId);

            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitTransactionAsync();
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync();

            return new ErrorDataResult<OrderDto>($"Sipariş oluşturulamadı: {ex.Message}");
        }

        var createdOrder = await _orderDal.GetByIdWithDetailsAsync(order.Id);
        await _auditService.LogActionAsync(
            userId.ToString(),
            "CreateOrder",
            "Order",
            new { OrderId = order.Id, OrderNumber = order.OrderNumber, TotalAmount = order.TotalAmount });
        
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
            return new ErrorDataResult<OrderDto>("Only 'Cancelled' status is supported via this endpoint logic currently.");

        var order = await _orderDal.GetByIdWithDetailsAsync(orderId);
        if (order == null || order.UserId != userId)
            return new ErrorDataResult<OrderDto>("Sipariş bulunamadı.");

        if (order.Status != OrderStatus.PendingPayment)
            return new ErrorDataResult<OrderDto>("Sadece ödeme bekleyen siparişler iptal edilebilir.");

        var previousStatus = order.Status;
        order.Status = OrderStatus.Cancelled;
        order.CancelledAt = DateTime.UtcNow;


        var stockReturns = order.OrderItems.ToDictionary(i => i.ProductId, i => i.Quantity);
        

        await _unitOfWork.BeginTransactionAsync();
        try
        {
            await _inventoryService.ReleaseStocksAsync(stockReturns, userId, $"Sipariş İptali - Sipariş No: {order.OrderNumber}");
            _orderDal.Update(order);
            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitTransactionAsync();
        }
        catch(Exception)
        {
            await _unitOfWork.RollbackTransactionAsync();
            throw; 
        }

        await _auditService.LogActionAsync(
            userId.ToString(),
            "CancelOrder",
            "Order",
            new { OrderId = order.Id, OrderNumber = order.OrderNumber, PreviousStatus = previousStatus.ToString() });

        return new SuccessDataResult<OrderDto>(order.ToDto(), "Sipariş iptal edildi.");
    }

    public async Task<IResult> CancelExpiredOrdersAsync()
    {
        var expiryTime = DateTime.UtcNow.AddMinutes(-OrderTimeoutMinutes);
        var expiredOrders = await _orderDal.GetExpiredPendingOrdersAsync(expiryTime);

        if (!expiredOrders.Any()) return new SuccessResult();


        foreach (var order in expiredOrders)
        {
            try
            {
                order.Status = OrderStatus.Cancelled;
                order.Notes = (order.Notes ?? "") + " | [Sistem] Ödeme zaman aşımı nedeniyle iptal edildi.";
                order.CancelledAt = DateTime.UtcNow;

                var stockReturns = order.OrderItems.ToDictionary(i => i.ProductId, i => i.Quantity);
                
                await _inventoryService.ReleaseStocksAsync(stockReturns, order.UserId, $"Sistem İptali - Sipariş No: {order.OrderNumber}");
                _orderDal.Update(order);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Sipariş iptal edilirken hata: OrderId={OrderId}", order.Id);
            }
        }
        
        await _unitOfWork.SaveChangesAsync();
        return new SuccessResult($"{expiredOrders.Count} adet zaman aşımına uğrayan sipariş iptal edildi.");
    }

    public async Task<IDataResult<List<OrderDto>>> GetAllOrdersAsync()
    {
        var orders = await _orderDal.GetAllOrdersWithDetailsAsync();
        return new SuccessDataResult<List<OrderDto>>(orders.Select(x => x.ToDto()).ToList());
    }

    public async Task<IDataResult<List<OrderDto>>> GetOrdersForSellerAsync(int sellerId)
    {
        var orders = await _orderDal.GetOrdersBySellerIdAsync(sellerId);
        return new SuccessDataResult<List<OrderDto>>(orders.Select(x => x.ToDto()).ToList());
    }

    public async Task<IDataResult<OrderDto>> UpdateOrderStatusAsync(int orderId, string status, int? sellerId = null)
    {
        var order = await _orderDal.GetByIdWithDetailsAsync(orderId);
        if (order == null) return new ErrorDataResult<OrderDto>("Sipariş bulunamadı.");

        if (sellerId.HasValue)
        {
            var isSellerOrder = order.OrderItems.Any(oi => oi.Product.SellerId == sellerId.Value);
            if (!isSellerOrder)
                return new ErrorDataResult<OrderDto>("Bu sipariş size ait ürünler içermiyor, durumunu güncelleyemezsiniz.");
        }

        if (!Enum.TryParse<OrderStatus>(status, true, out var orderStatus))
        {
            return new ErrorDataResult<OrderDto>($"Geçersiz sipariş durumu: {status}");
        }

        var previousStatus = order.Status;
        order.Status = orderStatus;
        _orderDal.Update(order);
        await _unitOfWork.SaveChangesAsync(); 

        await _auditService.LogActionAsync(
            sellerId?.ToString() ?? "Admin",
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

        var existingItems = order!.OrderItems.ToDictionary(oi => oi.ProductId, oi => oi);
        var newItems = request.Items.ToDictionary(i => i.ProductId, i => i);

        var (itemsToRemove, itemsToAdd, itemsToUpdate) = IdentifyChanges(existingItems, newItems);


        var stockValidation = ValidateStockAvailability(products, newItems, itemsToAdd, itemsToUpdate, existingItems);
        if (!stockValidation.Success) return new ErrorDataResult<OrderDto>(stockValidation.Message);

        var stockDeltas = new Dictionary<int, int>();

        foreach (var pid in itemsToRemove)
        {
            var qty = existingItems[pid].Quantity;
            if (stockDeltas.ContainsKey(pid)) stockDeltas[pid] += qty; else stockDeltas[pid] = qty;
        }

        foreach (var pid in itemsToAdd)
        {
            var qty = newItems[pid].Quantity;
            if (stockDeltas.ContainsKey(pid)) stockDeltas[pid] -= qty; else stockDeltas[pid] = -qty;
        }

        foreach (var pid in itemsToUpdate)
        {
            var oldQty = existingItems[pid].Quantity;
            var newQty = newItems[pid].Quantity;
            var diff = newQty - oldQty; 
            
            if (diff != 0)
            {
               if (stockDeltas.ContainsKey(pid)) stockDeltas[pid] -= diff; else stockDeltas[pid] = -diff; 
            }
        }

        await _unitOfWork.BeginTransactionAsync();
        try
        {
            if (stockDeltas.Any(x => x.Value != 0))
            {
                var stockResult = await _inventoryService.BulkAdjustStocksAsync(stockDeltas, userId, $"Sipariş Düzenleme - Sipariş No: {order.OrderNumber}");
                if (!stockResult.Success) throw new Exception(stockResult.Message);
            }

            ApplyOrderChanges(order, itemsToRemove, itemsToAdd, itemsToUpdate, newItems, products);
            RecalculateOrderTotals(order);
            
            _orderDal.Update(order);
            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitTransactionAsync();
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync();
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
        var productIds = items.Select(i => i.ProductId).Distinct().ToList();
        var products = await _productDal.GetByIdsWithInventoryAsync(productIds);
        return products.ToDictionary(p => p.Id, p => p);
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

    private void ApplyOrderChanges(
        Order order,
        List<int> itemsToRemove,
        List<int> itemsToAdd,
        List<int> itemsToUpdate,
        Dictionary<int, UpdateOrderItemDto> newItems,
        Dictionary<int, Product> products)
    {
        var existingItems = order.OrderItems.ToDictionary(i => i.ProductId);

        foreach (var pid in itemsToRemove)
        {
            order.OrderItems.Remove(existingItems[pid]);
        }

        foreach (var pid in itemsToAdd)
        {
            var product = products[pid];
            order.OrderItems.Add(new OrderItem
            {
                ProductId = pid,
                Quantity = newItems[pid].Quantity,
                PriceSnapshot = product.Price
            });
        }

        foreach (var pid in itemsToUpdate)
        {
            existingItems[pid].Quantity = newItems[pid].Quantity;
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

    private async Task ProcessCouponUsageAsync(Order order, int? couponId)
    {
        if (couponId.HasValue)
        {
            var usageResult = await _couponService.IncrementUsageAsync(couponId.Value);
            if (!usageResult.Success)
                throw new Exception("Kupon kullanımı kaydedilemedi.");
        }
    }
}
