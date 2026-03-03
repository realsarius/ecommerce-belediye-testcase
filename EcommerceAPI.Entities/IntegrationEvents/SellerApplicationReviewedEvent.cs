namespace EcommerceAPI.Entities.IntegrationEvents;

public sealed class SellerApplicationReviewedEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public int SellerProfileId { get; init; }
    public int UserId { get; init; }
    public string UserEmail { get; init; } = string.Empty;
    public string CustomerName { get; init; } = string.Empty;
    public string BrandName { get; init; } = string.Empty;
    public string Decision { get; init; } = string.Empty;
    public string? ReviewNote { get; init; }
    public DateTime ReviewedAt { get; init; } = DateTime.UtcNow;
}
