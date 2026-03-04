using EcommerceAPI.Entities.IntegrationEvents;

namespace EcommerceAPI.Business.Abstract;

public interface IRefundRetryScheduler
{
    bool TryScheduleRetry(RefundRequestedEvent failedMessage);
}
