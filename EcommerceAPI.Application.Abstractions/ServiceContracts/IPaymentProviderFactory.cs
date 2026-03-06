using EcommerceAPI.Entities.Enums;

namespace EcommerceAPI.Application.Abstractions.ServiceContracts;

public interface IPaymentProviderFactory
{
    IPaymentProvider GetProvider(PaymentProviderType providerType);
}
