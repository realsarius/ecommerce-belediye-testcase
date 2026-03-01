namespace EcommerceAPI.Entities.IntegrationEvents;

public sealed class RefundRequestedEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public int RefundRequestId { get; init; }
    public int ReturnRequestId { get; init; }
    public int OrderId { get; init; }
    public int UserId { get; init; }
    public decimal Amount { get; init; }
    public string Currency { get; init; } = "TRY";
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
}
