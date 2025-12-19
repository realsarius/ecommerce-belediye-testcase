using System.ComponentModel.DataAnnotations;

namespace EcommerceAPI.Core.DTOs;

public class ProcessPaymentRequest
{
    [Required(ErrorMessage = "Sipariş ID zorunludur")]
    public int OrderId { get; set; }

    public string? IdempotencyKey { get; set; }

    // Mock card info (sadece simülasyon için)
    public string? CardNumber { get; set; }
    public string? CardHolderName { get; set; }
    public string? ExpiryDate { get; set; }
    public string? CVV { get; set; }
}
