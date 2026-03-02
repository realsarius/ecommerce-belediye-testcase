namespace EcommerceAPI.Entities.Concrete;

public class GiftCard : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public decimal InitialBalance { get; set; }
    public decimal CurrentBalance { get; set; }
    public string Currency { get; set; } = "TRY";
    public bool IsActive { get; set; } = true;
    public DateTime? ExpiresAt { get; set; }
    public int? AssignedUserId { get; set; }
    public DateTime? AssignedAt { get; set; }
    public DateTime? LastUsedAt { get; set; }
    public string? Description { get; set; }

    public User? AssignedUser { get; set; }
    public ICollection<GiftCardTransaction> Transactions { get; set; } = new List<GiftCardTransaction>();
}
