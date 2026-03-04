using EcommerceAPI.Entities.Enums;

namespace EcommerceAPI.Infrastructure.Settings;

public class PaymentSettings
{
    public List<PaymentProviderType> ActiveProviders { get; set; } = [PaymentProviderType.Iyzico];
    public PaymentProviderType DefaultProvider { get; set; } = PaymentProviderType.Iyzico;
    public bool EnableMultiProviderSelection { get; set; } = true;
    public bool EnableTokenizedCardSave { get; set; } = true;
    public bool AllowLegacyEncryptedSavedCardPayments { get; set; } = true;
    public bool Force3DSecure { get; set; }
    public decimal Force3DSecureAbove { get; set; } = 5000m;
    public string PublicApiBaseUrl { get; set; } = "http://localhost:5294";
}
