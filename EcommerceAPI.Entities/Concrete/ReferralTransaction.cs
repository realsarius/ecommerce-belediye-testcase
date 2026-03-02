using EcommerceAPI.Entities.Enums;

namespace EcommerceAPI.Entities.Concrete;

public class ReferralTransaction : BaseEntity
{
    public int ReferralCodeId { get; set; }
    public int ReferrerUserId { get; set; }
    public int ReferredUserId { get; set; }
    public int? BeneficiaryUserId { get; set; }
    public int? OrderId { get; set; }
    public ReferralTransactionType Type { get; set; } = ReferralTransactionType.Signup;
    public int Points { get; set; }
    public string Description { get; set; } = string.Empty;

    public ReferralCode ReferralCode { get; set; } = null!;
    public User ReferrerUser { get; set; } = null!;
    public User ReferredUser { get; set; } = null!;
    public User? BeneficiaryUser { get; set; }
    public Order? Order { get; set; }
}
