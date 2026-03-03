namespace EcommerceAPI.Entities.IntegrationEvents;

public sealed class AnnouncementCreatedEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public int AnnouncementId { get; init; }
    public DateTime? ScheduledAt { get; init; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
}
