using EcommerceAPI.Business.Services.Abstract;
using EcommerceAPI.Core.DTOs;
using EcommerceAPI.Core.Entities;
using EcommerceAPI.Core.Enums;
using EcommerceAPI.Core.Exceptions;
using EcommerceAPI.Core.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EcommerceAPI.Business.Services.Concrete;

public class OrderService : IOrderService
{
    private readonly IOrderRepository _orderRepository;
    private readonly ICartRepository _cartRepository;
    private readonly IInventoryService _inventoryService;
    private readonly ICartService _cartService;
    private readonly IUnitOfWork _unitOfWork;

    public OrderService(
        IOrderRepository orderRepository,
        ICartRepository cartRepository,
        IInventoryService inventoryService,
        ICartService cartService,
        IUnitOfWork unitOfWork)
    {
        _orderRepository = orderRepository;
        _cartRepository = cartRepository;
        _inventoryService = inventoryService;
        _cartService = cartService;
        _unitOfWork = unitOfWork;
    }

    public async Task<OrderDto> CheckoutAsync(int userId, CheckoutRequest request)
    {
        var cart = await _cartRepository.GetActiveCartByUserIdAsync(userId);

        if (cart == null || !cart.Items.Any())
            throw new DomainException("Sepetiniz boş. Sipariş oluşturmak için sepete ürün ekleyin.");

        foreach (var item in cart.Items)
        {
            var availableStock = item.Product.Inventory?.QuantityAvailable ?? 0;
            if (item.Quantity > availableStock)
                throw new InsufficientStockException(item.ProductId, item.Quantity, availableStock);
        }

        var order = new Order
        {
            UserId = userId,
            OrderNumber = GenerateOrderNumber(),
            Status = OrderStatus.PendingPayment,
            ShippingAddress = request.ShippingAddress,
            Notes = request.Notes ?? string.Empty,
            TotalAmount = cart.Items.Sum(i => i.PriceSnapshot * i.Quantity)
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

        await _orderRepository.AddAsync(order);

        try
        {
            foreach (var cartItem in cart.Items)
            {
                await _inventoryService.DecreaseStockAsync(
                    cartItem.ProductId, 
                    cartItem.Quantity, 
                    userId, 
                    $"Satış - Sipariş No: {order.OrderNumber}");
            }

            await _cartService.ClearCartAsync(userId);

            await _unitOfWork.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new DomainException("Stok bilgisi güncellendi. Lütfen sepetinizi kontrol edip tekrar deneyin.");
        }

        var createdOrder = await _orderRepository.GetByIdWithDetailsAsync(order.Id);
        return MapToDto(createdOrder!);
    }

    public async Task<OrderDto> GetOrderAsync(int userId, int orderId)
    {
        var order = await _orderRepository.GetByIdWithDetailsAsync(orderId);

        if (order == null || order.UserId != userId)
            throw new NotFoundException("Sipariş", orderId);

        return MapToDto(order);
    }

    public async Task<List<OrderDto>> GetUserOrdersAsync(int userId)
    {
        var orders = await _orderRepository.GetUserOrdersAsync(userId);
        return orders.Select(MapToDto).ToList();
    }

    public async Task<OrderDto> CancelOrderAsync(int userId, int orderId)
    {
        var order = await _orderRepository.GetByIdWithDetailsAsync(orderId);

        if (order == null || order.UserId != userId)
            throw new NotFoundException("Sipariş", orderId);

        if (order.Status != OrderStatus.PendingPayment)
            throw new DomainException("Sadece ödeme bekleyen siparişler iptal edilebilir.");

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

        _orderRepository.Update(order);
        await _unitOfWork.SaveChangesAsync();

        return MapToDto(order);
    }

    public async Task CancelExpiredOrdersAsync()
    {
        // 30 dakikadan fazla PendingPayment durumunda bekleyen siparişleri al
        var expiredOrders = await _orderRepository.GetExpiredPendingOrdersAsync(30);

        if (!expiredOrders.Any()) return;

        foreach (var order in expiredOrders)
        {
            // Durumu güncelle
            order.Status = OrderStatus.Cancelled;
            order.Notes = (order.Notes ?? "") + " | [Sistem] Ödeme zaman aşımı nedeniyle iptal edildi.";
            order.CancelledAt = DateTime.UtcNow;

            // Stokları iade et
            foreach (var item in order.OrderItems)
            {
                await _inventoryService.IncreaseStockAsync(
                    item.ProductId,
                    item.Quantity,
                    order.UserId,
                    $"Sistem İptali - Sipariş No: {order.OrderNumber}");
            }

            _orderRepository.Update(order);
        }

        await _unitOfWork.SaveChangesAsync();
    }

    public async Task<List<OrderDto>> GetAllOrdersAsync()
    {
        var orders = await _orderRepository.GetAllWithDetailsAsync();
        return orders.Select(MapToDto).ToList();
    }

    public async Task<OrderDto> UpdateOrderStatusAsync(int orderId, string status)
    {
        var order = await _orderRepository.GetByIdWithDetailsAsync(orderId);
        if (order == null) throw new NotFoundException("Sipariş", orderId);

        if (!Enum.TryParse<OrderStatus>(status, true, out var orderStatus))
        {
            throw new DomainException($"Geçersiz sipariş durumu: {status}");
        }

        order.Status = orderStatus;
        _orderRepository.Update(order);
        await _unitOfWork.SaveChangesAsync();

        return MapToDto(order);
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
