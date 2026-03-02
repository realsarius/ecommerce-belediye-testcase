namespace EcommerceAPI.Entities.Concrete;

public class User : BaseEntity
{
    public string Email { get; set; } = string.Empty;
    
    public string EmailHash { get; set; } = string.Empty;
    
    public string PasswordHash { get; set; } = string.Empty;
    public string? GoogleSubject { get; set; }
    public string? AppleSubject { get; set; }
    public bool IsEmailVerified { get; set; }
    public int? ReferredByUserId { get; set; }
    public int? AppliedReferralCodeId { get; set; }
    public int? ReferralRewardedOrderId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public int RoleId { get; set; }
    
    public Role Role { get; set; } = null!;
    public User? ReferredByUser { get; set; }
    public ReferralCode? ReferralCode { get; set; }
    public SellerProfile? SellerProfile { get; set; }
    public ICollection<Order> Orders { get; set; } = new List<Order>();
    public Cart? Cart { get; set; }
    public ICollection<InventoryMovement> InventoryMovements { get; set; } = new List<InventoryMovement>();
    public ICollection<ShippingAddress> ShippingAddresses { get; set; } = new List<ShippingAddress>();
    public ICollection<CreditCard> CreditCards { get; set; } = new List<CreditCard>();
    public Wishlist? Wishlist { get; set; }
    public ICollection<LoyaltyTransaction> LoyaltyTransactions { get; set; } = new List<LoyaltyTransaction>();
    public ICollection<GiftCard> GiftCards { get; set; } = new List<GiftCard>();
    public ICollection<User> ReferredUsers { get; set; } = new List<User>();
    public ICollection<ReferralTransaction> ReferralTransactionsAsReferrer { get; set; } = new List<ReferralTransaction>();
    public ICollection<ReferralTransaction> ReferralTransactionsAsReferred { get; set; } = new List<ReferralTransaction>();
    public ICollection<ReferralTransaction> ReferralTransactionsAsBeneficiary { get; set; } = new List<ReferralTransaction>();
}
