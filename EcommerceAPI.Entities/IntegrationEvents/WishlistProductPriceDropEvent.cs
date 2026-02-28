namespace EcommerceAPI.Entities.IntegrationEvents;

public sealed class WishlistProductPriceDropEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public int UserId { get; init; }
    public int ProductId { get; init; }
    public string ProductName { get; init; } = string.Empty;
    public decimal TargetPrice { get; init; }
    public decimal OldPrice { get; init; }
    public decimal NewPrice { get; init; }
    public string Currency { get; init; } = "TRY";
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;
}
