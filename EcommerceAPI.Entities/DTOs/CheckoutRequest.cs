using EcommerceAPI.Core.Entities;

namespace EcommerceAPI.Entities.DTOs;

public class CheckoutRequest : IDto
{
    public string ShippingAddress { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
    public string? CouponCode { get; set; }
}
