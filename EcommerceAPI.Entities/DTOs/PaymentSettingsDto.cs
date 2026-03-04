using EcommerceAPI.Core.Entities;
using EcommerceAPI.Entities.Enums;

namespace EcommerceAPI.Entities.DTOs;

public class PaymentSettingsDto : IDto
{
    public List<PaymentProviderType> ActiveProviders { get; set; } = [];
    public PaymentProviderType DefaultProvider { get; set; } = PaymentProviderType.Iyzico;
    public bool EnableMultiProviderSelection { get; set; }
    public bool EnableTokenizedCardSave { get; set; }
    public bool AllowLegacyEncryptedSavedCardPayments { get; set; }
    public bool Force3DSecure { get; set; }
    public decimal Force3DSecureAbove { get; set; }
}
