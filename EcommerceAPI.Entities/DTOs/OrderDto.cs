using EcommerceAPI.Core.Entities;
namespace EcommerceAPI.Entities.DTOs;

public class OrderDto : IDto
{
    public int Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string ShippingAddress { get; set; } = string.Empty;
    public string? CustomerName { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? CancelledAt { get; set; }
    
    // Kupon bilgileri
    public string? CouponCode { get; set; }
    public decimal DiscountAmount { get; set; }
    
    public List<OrderItemDto> Items { get; set; } = new();
    public PaymentDto? Payment { get; set; }
}

