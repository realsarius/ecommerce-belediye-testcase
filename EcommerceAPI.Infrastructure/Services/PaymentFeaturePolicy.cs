using EcommerceAPI.Business.Abstract;
using EcommerceAPI.Infrastructure.Settings;
using Microsoft.Extensions.Options;

namespace EcommerceAPI.Infrastructure.Services;

public class PaymentFeaturePolicy : IPaymentFeaturePolicy
{
    private readonly PaymentSettings _paymentSettings;

    public PaymentFeaturePolicy(IOptions<PaymentSettings> paymentSettings)
    {
        _paymentSettings = paymentSettings.Value;
    }

    public bool AllowLegacyEncryptedSavedCardPayments => _paymentSettings.AllowLegacyEncryptedSavedCardPayments;
}
