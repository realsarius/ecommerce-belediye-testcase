using EcommerceAPI.Entities.Enums;
using EcommerceAPI.Core.Entities;
namespace EcommerceAPI.Entities.DTOs;

public class PaymentDto : IDto
{
    public int Id { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string PaymentMethod { get; set; } = string.Empty;
    public PaymentProviderType? Provider { get; set; }
    public string? Last4Digits { get; set; }
    public string? ErrorMessage { get; set; }
    public bool RequiresThreeDS { get; set; }
    public string? ThreeDSHtmlContent { get; set; }
    public DateTime CreatedAt { get; set; }
}
