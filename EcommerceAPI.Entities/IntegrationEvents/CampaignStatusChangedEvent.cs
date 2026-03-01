using EcommerceAPI.Entities.Enums;

namespace EcommerceAPI.Entities.IntegrationEvents;

public sealed record CampaignStatusChangedEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public int CampaignId { get; init; }
    public string CampaignName { get; init; } = string.Empty;
    public string? BadgeText { get; init; }
    public CampaignStatus PreviousStatus { get; init; }
    public CampaignStatus CurrentStatus { get; init; }
    public DateTime StartsAt { get; init; }
    public DateTime EndsAt { get; init; }
    public int ProductCount { get; init; }
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;
}
