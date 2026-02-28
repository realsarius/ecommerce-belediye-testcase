namespace EcommerceAPI.Entities.IntegrationEvents;

public sealed class WishlistProductLowStockEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public int ProductId { get; init; }
    public int StockQuantity { get; init; }
    public int Threshold { get; init; }
    public string Reason { get; init; } = string.Empty;
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;
}
