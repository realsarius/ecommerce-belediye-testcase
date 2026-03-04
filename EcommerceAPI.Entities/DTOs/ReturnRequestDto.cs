using EcommerceAPI.Core.Entities;
using EcommerceAPI.Entities.Enums;

namespace EcommerceAPI.Entities.DTOs;

public class ReturnRequestDto : IDto
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public int UserId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string ReasonCategory { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public string? RequestNote { get; set; }
    public DateTime? RequestWindowEndsAt { get; set; }
    public List<ReturnRequestItemDto> SelectedItems { get; set; } = new();
    public List<ReturnRequestAttachmentDto> Attachments { get; set; } = new();
    public decimal RequestedRefundAmount { get; set; }
    public string? PaymentStatus { get; set; }
    public int? ReviewedByUserId { get; set; }
    public string? ReviewerName { get; set; }
    public string? ReviewNote { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public int? RefundRequestId { get; set; }
    public PaymentProviderType? RefundProvider { get; set; }
    public string? RefundStatus { get; set; }
    public DateTime CreatedAt { get; set; }
}
