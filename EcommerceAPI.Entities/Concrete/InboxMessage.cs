namespace EcommerceAPI.Entities.Concrete;

public class InboxMessage : BaseEntity
{
    public Guid MessageId { get; set; }
    public string ConsumerName { get; set; } = string.Empty;
    public string MessageType { get; set; } = string.Empty;
    public DateTime ProcessedOnUtc { get; set; } = DateTime.UtcNow;
}
