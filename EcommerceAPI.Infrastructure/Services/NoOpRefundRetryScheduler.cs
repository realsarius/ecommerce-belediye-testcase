using EcommerceAPI.Business.Abstract;
using EcommerceAPI.Entities.IntegrationEvents;

namespace EcommerceAPI.Infrastructure.Services;

public class NoOpRefundRetryScheduler : IRefundRetryScheduler
{
    public bool TryScheduleRetry(RefundRequestedEvent failedMessage)
    {
        return false;
    }
}
