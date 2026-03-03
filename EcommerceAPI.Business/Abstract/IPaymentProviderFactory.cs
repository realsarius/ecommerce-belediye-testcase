using EcommerceAPI.Entities.Enums;

namespace EcommerceAPI.Business.Abstract;

public interface IPaymentProviderFactory
{
    IPaymentProvider GetProvider(PaymentProviderType providerType);
}
