using EcommerceAPI.Entities.Enums;

namespace EcommerceAPI.Entities.Concrete;

public class LoyaltyTransaction : BaseEntity
{
    public int UserId { get; set; }
    public int? OrderId { get; set; }
    public LoyaltyTransactionType Type { get; set; } = LoyaltyTransactionType.Earned;
    public int Points { get; set; }
    public int BalanceAfter { get; set; }
    public string Description { get; set; } = string.Empty;
    public DateTime? ExpiresAt { get; set; }

    public User User { get; set; } = null!;
    public Order? Order { get; set; }
}
