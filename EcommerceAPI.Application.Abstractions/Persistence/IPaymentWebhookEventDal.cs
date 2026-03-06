using EcommerceAPI.Core.DataAccess;
using EcommerceAPI.Entities.Concrete;

namespace EcommerceAPI.Application.Abstractions.Persistence;

public interface IPaymentWebhookEventDal : IEntityRepository<PaymentWebhookEvent>
{
    Task<bool> TryAddWebhookEventAsync(PaymentWebhookEvent webhookEvent, CancellationToken cancellationToken = default);
}
