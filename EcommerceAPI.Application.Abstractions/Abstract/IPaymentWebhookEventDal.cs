using EcommerceAPI.Core.DataAccess;
using EcommerceAPI.Entities.Concrete;

namespace EcommerceAPI.DataAccess.Abstract;

public interface IPaymentWebhookEventDal : IEntityRepository<PaymentWebhookEvent>
{
    Task<bool> TryAddWebhookEventAsync(PaymentWebhookEvent webhookEvent, CancellationToken cancellationToken = default);
}
