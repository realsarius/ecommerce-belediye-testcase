using EcommerceAPI.Core.Entities;
namespace EcommerceAPI.Entities.DTOs;


public class IyzicoWebhookRequest : IDto
{
    public string? IyziEventType { get; set; }
    
    public string? PaymentId { get; set; }
    
    public string? PaymentConversationId { get; set; }
    
    public string? Status { get; set; }
    
    public string? IyziReferenceCode { get; set; }
    
    public string? IyziEventTime { get; set; }
    
    public string? IyziPaymentId { get; set; }
    
    public string? Token { get; set; }
}
