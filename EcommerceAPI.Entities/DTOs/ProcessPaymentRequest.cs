using EcommerceAPI.Core.Entities;
using System.ComponentModel.DataAnnotations;

namespace EcommerceAPI.Entities.DTOs;

public class ProcessPaymentRequest : IDto
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
