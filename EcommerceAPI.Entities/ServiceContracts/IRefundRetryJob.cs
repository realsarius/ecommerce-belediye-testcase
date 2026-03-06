using EcommerceAPI.Entities.IntegrationEvents;

namespace EcommerceAPI.Business.Abstract;

public interface IRefundRetryJob
{
    Task PublishRetryAsync(RefundRequestedEvent message);
}
