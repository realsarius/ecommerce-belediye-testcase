namespace EcommerceAPI.Business.Abstract;

public interface IPaymentFeaturePolicy
{
    bool AllowLegacyEncryptedSavedCardPayments { get; }
}
