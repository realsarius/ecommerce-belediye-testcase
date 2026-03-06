using EcommerceAPI.Entities.IntegrationEvents;

namespace EcommerceAPI.Application.Abstractions.ServiceContracts;

public interface IRefundRetryScheduler
{
    bool TryScheduleRetry(RefundRequestedEvent failedMessage);
}
