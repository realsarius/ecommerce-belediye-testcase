using EcommerceAPI.Entities.IntegrationEvents;

namespace EcommerceAPI.Application.Abstractions.ServiceContracts;

public interface IRefundRetryJob
{
    Task PublishRetryAsync(RefundRequestedEvent message);
}
