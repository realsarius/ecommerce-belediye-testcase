using EcommerceAPI.Entities.Enums;

namespace EcommerceAPI.Application.Abstractions.ServiceContracts;

public interface IRefundProviderFactory
{
    IRefundProvider GetProvider(PaymentProviderType providerType);
}
