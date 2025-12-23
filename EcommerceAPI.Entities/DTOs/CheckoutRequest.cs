using EcommerceAPI.Core.Entities;
using System.ComponentModel.DataAnnotations;

namespace EcommerceAPI.Entities.DTOs;

public class CheckoutRequest : IDto
{
    [Required(ErrorMessage = "Teslimat adresi zorunludur")]
    [StringLength(500, MinimumLength = 10, ErrorMessage = "Teslimat adresi 10-500 karakter arasında olmalıdır")]
    public string ShippingAddress { get; set; } = string.Empty;

    [StringLength(1000, ErrorMessage = "Notlar 1000 karakteri geçemez")]
    public string? Notes { get; set; }

    [Required(ErrorMessage = "Ödeme yöntemi zorunludur")]
    public string PaymentMethod { get; set; } = string.Empty;
}
