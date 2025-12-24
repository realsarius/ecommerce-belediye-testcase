using EcommerceAPI.Entities.Enums;

namespace EcommerceAPI.Entities.Concrete;

public class Payment : BaseEntity
{
    public int OrderId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "TRY";
    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;
    public string PaymentMethod { get; set; } = string.Empty; 
    public string? PaymentProviderId { get; set; }
    public string IdempotencyKey { get; set; } = string.Empty;
    public string? ErrorMessage { get; set; }
    
    // Navigation properties
    public Order Order { get; set; } = null!;
}
