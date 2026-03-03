namespace EcommerceAPI.Entities.IntegrationEvents;

public sealed class OrderShippedEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public int OrderId { get; init; }
    public string OrderNumber { get; init; } = string.Empty;
    public int UserId { get; init; }
    public string CustomerEmail { get; init; } = string.Empty;
    public string CustomerName { get; init; } = string.Empty;
    public string CargoCompany { get; init; } = string.Empty;
    public string TrackingCode { get; init; } = string.Empty;
    public DateTime ShippedAt { get; init; } = DateTime.UtcNow;
}
