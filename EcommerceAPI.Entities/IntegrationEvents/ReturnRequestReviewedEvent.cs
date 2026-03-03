namespace EcommerceAPI.Entities.IntegrationEvents;

public sealed class ReturnRequestReviewedEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public int ReturnRequestId { get; init; }
    public int OrderId { get; init; }
    public int UserId { get; init; }
    public string UserEmail { get; init; } = string.Empty;
    public string CustomerName { get; init; } = string.Empty;
    public string OrderNumber { get; init; } = string.Empty;
    public string Decision { get; init; } = string.Empty;
    public string CurrentStatus { get; init; } = string.Empty;
    public string? ReviewNote { get; init; }
    public DateTime ReviewedAt { get; init; } = DateTime.UtcNow;
}
