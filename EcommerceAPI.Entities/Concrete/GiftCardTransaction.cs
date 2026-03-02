using EcommerceAPI.Entities.Enums;

namespace EcommerceAPI.Entities.Concrete;

public class GiftCardTransaction : BaseEntity
{
    public int GiftCardId { get; set; }
    public int? UserId { get; set; }
    public int? OrderId { get; set; }
    public GiftCardTransactionType Type { get; set; }
    public decimal Amount { get; set; }
    public decimal BalanceAfter { get; set; }
    public string Description { get; set; } = string.Empty;

    public GiftCard GiftCard { get; set; } = null!;
    public User? User { get; set; }
    public Order? Order { get; set; }
}
