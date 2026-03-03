using EcommerceAPI.Entities.Enums;

namespace EcommerceAPI.Entities.DTOs;

public class CreditCardDto
{
    public int Id { get; set; }
    public string CardAlias { get; set; } = string.Empty;
    public string CardHolderName { get; set; } = string.Empty;
    public string Last4Digits { get; set; } = string.Empty;
    public string ExpireMonth { get; set; } = string.Empty;
    public string ExpireYear { get; set; } = string.Empty;
    public bool IsTokenized { get; set; }
    public PaymentProviderType? TokenProvider { get; set; }
    public bool IsDefault { get; set; }
}

public class AddCreditCardRequest
{
    public string CardAlias { get; set; } = string.Empty;
    public string CardHolderName { get; set; } = string.Empty;
    public string CardNumber { get; set; } = string.Empty;
    public string ExpireMonth { get; set; } = string.Empty;
    public string ExpireYear { get; set; } = string.Empty;
    public bool IsDefault { get; set; } = false;
}

public class SaveTokenizedCreditCardRequest
{
    public string CardAlias { get; set; } = string.Empty;
    public string CardHolderName { get; set; } = string.Empty;
    public string Last4Digits { get; set; } = string.Empty;
    public string ExpireMonth { get; set; } = string.Empty;
    public string ExpireYear { get; set; } = string.Empty;
    public PaymentProviderType TokenProvider { get; set; }
    public string? IyzicoCardToken { get; set; }
    public string? IyzicoUserKey { get; set; }
    public bool IsDefault { get; set; }
}

/// <summary>
/// Ödeme işlemi için kullanılan kayıtlı kart bilgileri.
/// DİKKAT: Bu DTO sadece ödeme servisleri arasında internal kullanım içindir,
/// asla API response olarak döndürülmemelidir!
/// </summary>
public class StoredCardPaymentDto
{
    public int Id { get; set; }
    public string CardHolderName { get; set; } = string.Empty;
    public string? CardNumber { get; set; }
    public string ExpireMonth { get; set; } = string.Empty;
    public string ExpireYear { get; set; } = string.Empty;
    public bool IsTokenized { get; set; }
    public PaymentProviderType? TokenProvider { get; set; }
    public string? IyzicoCardToken { get; set; }
    public string? IyzicoUserKey { get; set; }
}
