namespace EcommerceAPI.Entities.Concrete;

public class OutboxMessage : BaseEntity
{
    public Guid EventId { get; set; } = Guid.NewGuid();
    public string EventType { get; set; } = string.Empty;
    public string Payload { get; set; } = string.Empty;
    public DateTime? ProcessedOnUtc { get; set; }
    public int RetryCount { get; set; }
    public string? LastError { get; set; }
}
