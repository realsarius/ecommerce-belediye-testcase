namespace EcommerceAPI.Entities.IntegrationEvents;

public sealed class OrderCreatedEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public int OrderId { get; init; }
    public string OrderNumber { get; init; } = string.Empty;
    public int UserId { get; init; }
    public decimal TotalAmount { get; init; }
    public string Currency { get; init; } = "TRY";
    public string? IdempotencyKey { get; init; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
}
