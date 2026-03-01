namespace EcommerceAPI.Entities.IntegrationEvents;

public class ContactMessageReceivedEvent
{
    public Guid EventId { get; set; } = Guid.NewGuid();
    public int ContactMessageId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
