using EcommerceAPI.Business.Abstract;
using EcommerceAPI.Entities.DTOs;
using EcommerceAPI.Core.Utilities.Results;
using EcommerceAPI.DataAccess.Abstract;
using EcommerceAPI.Entities.Concrete;
using EcommerceAPI.Entities.Enums;
using EcommerceAPI.Core.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EcommerceAPI.Business.Concrete;

public class OrderManager : IOrderService
{
    private readonly IOrderDal _orderDal;
    private readonly ICartDal _cartDal;
    private readonly IInventoryService _inventoryService;
    private readonly ICartService _cartService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICouponService _couponService;

    public OrderManager(
        IOrderDal orderDal,
        ICartDal cartDal,
        IInventoryService inventoryService,
        ICartService cartService,
        IUnitOfWork unitOfWork,
        ICouponService couponService)
    {
        _orderDal = orderDal;
        _cartDal = cartDal;
        _inventoryService = inventoryService;
        _cartService = cartService;
        _unitOfWork = unitOfWork;
        _couponService = couponService;
    }

    public async Task<IDataResult<OrderDto>> CheckoutAsync(int userId, CheckoutRequest request)
    {
        var cart = await _cartDal.GetByUserIdWithItemsAsync(userId);
        
        if (cart == null || !cart.Items.Any())
            return new ErrorDataResult<OrderDto>("Sepetiniz boş. Sipariş oluşturmak için sepete ürün ekleyin.");

        foreach (var item in cart.Items)
        {
            var availableStock = item.Product.Inventory?.QuantityAvailable ?? 0;
            if (item.Quantity > availableStock)
                return new ErrorDataResult<OrderDto>($"Stok yetersiz: {item.Product.Name}");
        }

        // Ara toplam hesapla
        var subtotal = cart.Items.Sum(i => i.PriceSnapshot * i.Quantity);
        
        // Kargo Hesaplama (1000 TL üzeri ücretsiz)
        decimal shippingCost = subtotal >= 1000 ? 0 : 29.90m;
        
        // Kupon validasyonu
        int? couponId = null;
        string? couponCode = null;
        decimal discountAmount = 0;
        
        if (!string.IsNullOrEmpty(request.CouponCode))
        {
            var couponResult = await _couponService.ValidateCouponAsync(request.CouponCode, subtotal);
            if (!couponResult.Success)
                return new ErrorDataResult<OrderDto>("Kupon doğrulanamadı.");
            
            var validation = couponResult.Data!;
            if (!validation.IsValid)
                return new ErrorDataResult<OrderDto>(validation.ErrorMessage ?? "Geçersiz kupon kodu.");
            
            couponId = validation.Coupon!.Id;
            couponCode = validation.Coupon.Code; // Snapshot
            discountAmount = validation.DiscountAmount;
        }

        var order = new Order
        {
            UserId = userId,
            OrderNumber = GenerateOrderNumber(),
            Status = OrderStatus.PendingPayment,
            ShippingAddress = request.ShippingAddress,
            Notes = request.Notes ?? string.Empty,
            TotalAmount = subtotal - discountAmount + shippingCost, // Kargo dahil
            CouponId = couponId,
            CouponCode = couponCode,
            DiscountAmount = discountAmount
        };

        foreach (var cartItem in cart.Items)
        {
            order.OrderItems.Add(new OrderItem
            {
                ProductId = cartItem.ProductId,
                Quantity = cartItem.Quantity,
                PriceSnapshot = cartItem.PriceSnapshot
            });
        }

        order.Payment = new Payment
        {
            Amount = order.TotalAmount,
            Status = PaymentStatus.Pending,
            PaymentMethod = request.PaymentMethod,
            IdempotencyKey = Guid.NewGuid().ToString()
        };

        await _orderDal.AddAsync(order);

        try
        {
            // Stok düşürme
            foreach (var cartItem in cart.Items)
            {
                var stockResult = await _inventoryService.DecreaseStockAsync(
                    cartItem.ProductId, 
                    cartItem.Quantity, 
                    userId, 
                    $"Satış - Sipariş No: {order.OrderNumber}");
                
                if (!stockResult.Success)
                     throw new Exception(stockResult.Message);
            }

            // Kupon kullanım sayısını artır (transaction içinde)
            if (couponId.HasValue)
            {
                var usageResult = await _couponService.IncrementUsageAsync(couponId.Value);
                if (!usageResult.Success)
                    throw new Exception("Kupon kullanımı kaydedilemedi.");
            }

            var clearCartResult = await _cartService.ClearCartAsync(userId);
             if (!clearCartResult.Success)
                 throw new Exception(clearCartResult.Message);

            await _unitOfWork.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            return new ErrorDataResult<OrderDto>($"Sipariş oluşturulamadı: {ex.Message}");
        }

        var createdOrder = await _orderDal.GetByIdWithDetailsAsync(order.Id);
        return new SuccessDataResult<OrderDto>(MapToDto(createdOrder!), "Sipariş başarıyla oluşturuldu.");
    }

    public async Task<IDataResult<OrderDto>> GetOrderAsync(int userId, int orderId)
    {
        var order = await _orderDal.GetByIdWithDetailsAsync(orderId);

        if (order == null || order.UserId != userId)
            return new ErrorDataResult<OrderDto>("Sipariş bulunamadı.");

        return new SuccessDataResult<OrderDto>(MapToDto(order));
    }

    public async Task<IDataResult<List<OrderDto>>> GetUserOrdersAsync(int userId)
    {
        var orders = await _orderDal.GetUserOrdersAsync(userId);
        return new SuccessDataResult<List<OrderDto>>(orders.Select(MapToDto).ToList());
    }

    public async Task<IDataResult<OrderDto>> CancelOrderAsync(int userId, int orderId)
    {
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

        return new SuccessDataResult<OrderDto>(MapToDto(order), "Sipariş iptal edildi.");
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
        return new SuccessDataResult<List<OrderDto>>(orders.Select(MapToDto).ToList());
    }

    public async Task<IDataResult<OrderDto>> UpdateOrderStatusAsync(int orderId, string status)
    {
        var order = await _orderDal.GetByIdWithDetailsAsync(orderId);
        if (order == null) return new ErrorDataResult<OrderDto>("Sipariş bulunamadı.");

        if (!Enum.TryParse<OrderStatus>(status, true, out var orderStatus))
        {
            return new ErrorDataResult<OrderDto>($"Geçersiz sipariş durumu: {status}");
        }

        order.Status = orderStatus;
        _orderDal.Update(order);
        await _unitOfWork.SaveChangesAsync();

        return new SuccessDataResult<OrderDto>(MapToDto(order), "Sipariş durumu güncellendi.");
    }

    private static string GenerateOrderNumber()
    {
        return $"ORD-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..8].ToUpper()}";
    }

    private static OrderDto MapToDto(Order order)
    {
        return new OrderDto
        {
            Id = order.Id,
            OrderNumber = order.OrderNumber,
            Status = order.Status.ToString(),
            TotalAmount = order.TotalAmount,
            Currency = order.Currency,
            ShippingAddress = order.ShippingAddress,
            CustomerName = order.User != null ? $"{order.User.FirstName} {order.User.LastName}" : "Bilinmiyor",
            Notes = order.Notes,
            CreatedAt = order.CreatedAt,
            CancelledAt = order.CancelledAt,
            CouponCode = order.CouponCode,
            DiscountAmount = order.DiscountAmount,
            Items = order.OrderItems.Select(oi => new OrderItemDto
            {
                Id = oi.Id,
                ProductId = oi.ProductId,
                ProductName = oi.Product?.Name ?? string.Empty,
                ProductSKU = oi.Product?.SKU ?? string.Empty,
                Quantity = oi.Quantity,
                PriceSnapshot = oi.PriceSnapshot,
                LineTotal = oi.Quantity * oi.PriceSnapshot
            }).ToList(),
            Payment = order.Payment != null ? new PaymentDto
            {
                Id = order.Payment.Id,
                Amount = order.Payment.Amount,
                Currency = order.Payment.Currency,
                Status = order.Payment.Status.ToString(),
                PaymentMethod = order.Payment.PaymentMethod,
                CreatedAt = order.Payment.CreatedAt
            } : null
        };
    }
}
