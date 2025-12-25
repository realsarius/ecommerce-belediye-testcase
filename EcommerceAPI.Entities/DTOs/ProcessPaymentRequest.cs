using EcommerceAPI.Core.Entities;

namespace EcommerceAPI.Entities.DTOs;

public class ProcessPaymentRequest : IDto
{
    public int OrderId { get; set; }
    public string? IdempotencyKey { get; set; }


    public int? SavedCardId { get; set; }


    public string? CardNumber { get; set; }
    public string? CardHolderName { get; set; }
    public string? ExpiryDate { get; set; }
    public string? CVV { get; set; }
}
