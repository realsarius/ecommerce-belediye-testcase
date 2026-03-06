using EcommerceAPI.Core.DataAccess;
using EcommerceAPI.Entities.Concrete;
using EcommerceAPI.Entities.Enums;

namespace EcommerceAPI.DataAccess.Abstract;

public interface IPaymentWebhookEventDal : IEntityRepository<PaymentWebhookEvent>
{
    Task<bool> ExistsByDedupeKeyAsync(PaymentProviderType provider, string dedupeKey);
}
