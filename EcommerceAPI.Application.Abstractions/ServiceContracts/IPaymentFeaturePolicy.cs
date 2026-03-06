namespace EcommerceAPI.Application.Abstractions.ServiceContracts;

public interface IPaymentFeaturePolicy
{
    bool AllowLegacyEncryptedSavedCardPayments { get; }
}
