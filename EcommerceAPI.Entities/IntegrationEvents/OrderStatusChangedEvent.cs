namespace EcommerceAPI.Entities.IntegrationEvents;

public sealed class OrderStatusChangedEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public int OrderId { get; init; }
    public string OrderNumber { get; init; } = string.Empty;
    public int UserId { get; init; }
    public string CustomerEmail { get; init; } = string.Empty;
    public string CustomerName { get; init; } = string.Empty;
    public string PreviousStatus { get; init; } = string.Empty;
    public string NewStatus { get; init; } = string.Empty;
    public DateTime ChangedAt { get; init; } = DateTime.UtcNow;
}
