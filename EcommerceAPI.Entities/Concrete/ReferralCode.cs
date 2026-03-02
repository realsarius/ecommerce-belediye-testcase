namespace EcommerceAPI.Entities.Concrete;

public class ReferralCode : BaseEntity
{
    public int UserId { get; set; }
    public string Code { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;

    public User User { get; set; } = null!;
    public ICollection<ReferralTransaction> ReferralTransactions { get; set; } = new List<ReferralTransaction>();
}
