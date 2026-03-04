using EcommerceAPI.Entities.Enums;

namespace EcommerceAPI.Business.Abstract;

public interface IRefundProviderFactory
{
    IRefundProvider GetProvider(PaymentProviderType providerType);
}
