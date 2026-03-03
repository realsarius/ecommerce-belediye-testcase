using EcommerceAPI.Entities.Enums;

namespace EcommerceAPI.Entities.Concrete;

public class CreditCard : BaseEntity
{
    public int UserId { get; set; }
    public string CardAlias { get; set; } = string.Empty;
    public string CardHolderName { get; set; } = string.Empty;
    public string CardNumberEncrypted { get; set; } = string.Empty;
    public string Last4Digits { get; set; } = string.Empty;
    public string ExpireYearEncrypted { get; set; } = string.Empty;
    public string ExpireMonthEncrypted { get; set; } = string.Empty;
    public string? IyzicoCardToken { get; set; }
    public string? IyzicoUserKey { get; set; }
    public string? StripePaymentMethodId { get; set; }
    public string? StripeCustomerId { get; set; }
    public string? PayTrToken { get; set; }
    public PaymentProviderType? TokenProvider { get; set; }
    public bool IsDefault { get; set; } = false;
    
    public User User { get; set; } = null!;
}
