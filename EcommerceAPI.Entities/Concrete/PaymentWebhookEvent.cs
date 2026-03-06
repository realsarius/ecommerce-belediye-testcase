using EcommerceAPI.Entities.Enums;

namespace EcommerceAPI.Entities.Concrete;

public class PaymentWebhookEvent : BaseEntity
{
    public PaymentProviderType Provider { get; set; } = PaymentProviderType.Iyzico;
    public string DedupeKey { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public string? ProviderEventId { get; set; }
    public string? PaymentId { get; set; }
    public string? PaymentConversationId { get; set; }
    public string? Status { get; set; }
    public string? EventTime { get; set; }
}
