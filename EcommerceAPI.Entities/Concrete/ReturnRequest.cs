using EcommerceAPI.Entities.Enums;

namespace EcommerceAPI.Entities.Concrete;

public class ReturnRequest : BaseEntity
{
    public int OrderId { get; set; }
    public int UserId { get; set; }
    public ReturnRequestType Type { get; set; } = ReturnRequestType.Return;
    public ReturnRequestStatus Status { get; set; } = ReturnRequestStatus.Pending;
    public string Reason { get; set; } = string.Empty;
    public string? RequestNote { get; set; }
    public decimal RequestedRefundAmount { get; set; }
    public int? ReviewedByUserId { get; set; }
    public string? ReviewNote { get; set; }
    public DateTime? ReviewedAt { get; set; }

    public Order Order { get; set; } = null!;
    public User User { get; set; } = null!;
    public User? ReviewedByUser { get; set; }
    public RefundRequest? RefundRequest { get; set; }
}
