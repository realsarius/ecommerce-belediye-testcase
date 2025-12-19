namespace EcommerceAPI.Core.DTOs;

public class PaymentDto
{
    public int Id { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string PaymentMethod { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
