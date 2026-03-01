using EcommerceAPI.Core.Entities;

namespace EcommerceAPI.Entities.DTOs;

public class RefundRequestDto : IDto
{
    public int Id { get; set; }
    public int ReturnRequestId { get; set; }
    public int OrderId { get; set; }
    public int UserId { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "TRY";
    public string Status { get; set; } = string.Empty;
    public string? ProviderRefundId { get; set; }
    public string? FailureReason { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public DateTime CreatedAt { get; set; }
}
