using EcommerceAPI.Business.Abstract;
using EcommerceAPI.Entities.Enums;

namespace EcommerceAPI.Infrastructure.Services;

public class RefundProviderFactory : IRefundProviderFactory
{
    private readonly IReadOnlyDictionary<PaymentProviderType, IRefundProvider> _providers;

    public RefundProviderFactory(IEnumerable<IRefundProvider> providers)
    {
        _providers = providers.ToDictionary(provider => provider.ProviderType);
    }

    public IRefundProvider GetProvider(PaymentProviderType providerType)
    {
        if (_providers.TryGetValue(providerType, out var provider))
        {
            return provider;
        }

        throw new NotSupportedException($"Refund saglayicisi desteklenmiyor: {providerType}");
    }
}
