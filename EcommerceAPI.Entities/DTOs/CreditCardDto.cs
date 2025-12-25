namespace EcommerceAPI.Entities.DTOs;

public class CreditCardDto
{
    public int Id { get; set; }
    public string CardAlias { get; set; } = string.Empty;
    public string CardHolderName { get; set; } = string.Empty;
    public string Last4Digits { get; set; } = string.Empty;
    public string ExpireMonth { get; set; } = string.Empty;
    public string ExpireYear { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
}

public class AddCreditCardRequest
{
    public string CardAlias { get; set; } = string.Empty;
    public string CardHolderName { get; set; } = string.Empty;
    public string CardNumber { get; set; } = string.Empty;
    public string ExpireMonth { get; set; } = string.Empty;
    public string ExpireYear { get; set; } = string.Empty;
    public string Cvv { get; set; } = string.Empty;
    public bool IsDefault { get; set; } = false;
}

/// <summary>
/// Ödeme işlemi için kullanılan şifresi çözülmüş kart bilgileri.
/// DİKKAT: Bu DTO sadece ödeme servisleri arasında internal kullanım içindir,
/// asla API response olarak döndürülmemelidir!
/// </summary>
public class DecryptedCardDto
{
    public int Id { get; set; }
    public string CardHolderName { get; set; } = string.Empty;
    public string CardNumber { get; set; } = string.Empty;
    public string ExpireMonth { get; set; } = string.Empty;
    public string ExpireYear { get; set; } = string.Empty;
}
