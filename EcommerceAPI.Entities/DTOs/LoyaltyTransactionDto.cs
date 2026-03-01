using EcommerceAPI.Core.Entities;

namespace EcommerceAPI.Entities.DTOs;

public class LoyaltyTransactionDto : IDto
{
    public int Id { get; set; }
    public int? OrderId { get; set; }
    public string? OrderNumber { get; set; }
    public string Type { get; set; } = string.Empty;
    public int Points { get; set; }
    public int BalanceAfter { get; set; }
    public string Description { get; set; } = string.Empty;
    public DateTime? ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; }
}
