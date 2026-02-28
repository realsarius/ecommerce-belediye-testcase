namespace EcommerceAPI.Entities.IntegrationEvents;

public sealed class WishlistItemRemovedEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public int UserId { get; init; }
    public int WishlistId { get; init; }
    public int ProductId { get; init; }
    public string Reason { get; init; } = "RemoveItem";
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;
}
