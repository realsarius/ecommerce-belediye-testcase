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
    public string CvvEncrypted { get; set; } = string.Empty;
    public bool IsDefault { get; set; } = false;
    
    public User User { get; set; } = null!;
}
