namespace EcommerceAPI.Entities.IntegrationEvents;

public sealed class WishlistItemAddedEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public int UserId { get; init; }
    public int WishlistId { get; init; }
    public int ProductId { get; init; }
    public decimal PriceAtTime { get; init; }
    public string Currency { get; init; } = "TRY";
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;
}
