using EcommerceAPI.Entities.Enums;

namespace EcommerceAPI.Entities.Concrete;

public class RefundRequest : BaseEntity
{
    public int ReturnRequestId { get; set; }
    public int OrderId { get; set; }
    public int? PaymentId { get; set; }
    public decimal Amount { get; set; }
    public RefundRequestStatus Status { get; set; } = RefundRequestStatus.Pending;
    public string IdempotencyKey { get; set; } = string.Empty;
    public string? ProviderRefundId { get; set; }
    public string? FailureReason { get; set; }
    public DateTime? ProcessedAt { get; set; }

    public ReturnRequest ReturnRequest { get; set; } = null!;
    public Order Order { get; set; } = null!;
    public Payment? Payment { get; set; }
}
