namespace EcommerceAPI.Entities.IntegrationEvents;

public static class ProductIndexOperations
{
    public const string Upsert = "Upsert";
    public const string Delete = "Delete";
}

public sealed class ProductIndexSyncEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public int ProductId { get; init; }
    public string Operation { get; init; } = ProductIndexOperations.Upsert;
    public string? Reason { get; init; }
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;
}
