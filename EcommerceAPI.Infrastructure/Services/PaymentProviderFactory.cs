using EcommerceAPI.Application.Abstractions.ServiceContracts;
using EcommerceAPI.Entities.Enums;

namespace EcommerceAPI.Infrastructure.Services;

public class PaymentProviderFactory : IPaymentProviderFactory
{
    private readonly IReadOnlyDictionary<PaymentProviderType, IPaymentProvider> _providers;

    public PaymentProviderFactory(IEnumerable<IPaymentProvider> providers)
    {
        _providers = providers.ToDictionary(provider => provider.ProviderType);
    }

    public IPaymentProvider GetProvider(PaymentProviderType providerType)
    {
        if (_providers.TryGetValue(providerType, out var provider))
        {
            return provider;
        }

        throw new NotSupportedException($"Odeme saglayicisi desteklenmiyor: {providerType}");
    }
}
