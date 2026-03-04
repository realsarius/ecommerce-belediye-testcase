using EcommerceAPI.Business.Abstract;
using EcommerceAPI.Entities.IntegrationEvents;
using EcommerceAPI.Infrastructure.Settings;
using Hangfire;
using Hangfire.Common;
using Hangfire.States;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EcommerceAPI.Infrastructure.Services;

public class HangfireRefundRetryScheduler : IRefundRetryScheduler
{
    private readonly IBackgroundJobClient _backgroundJobClient;
    private readonly RefundRetrySettings _settings;
    private readonly ILogger<HangfireRefundRetryScheduler> _logger;

    public HangfireRefundRetryScheduler(
        IBackgroundJobClient backgroundJobClient,
        IOptions<RefundRetrySettings> settings,
        ILogger<HangfireRefundRetryScheduler> logger)
    {
        _backgroundJobClient = backgroundJobClient;
        _settings = settings.Value;
        _logger = logger;
    }

    public bool TryScheduleRetry(RefundRequestedEvent failedMessage)
    {
        if (!_settings.Enabled)
        {
            return false;
        }

        var nextAttempt = failedMessage.RetryAttempt + 1;
        if (nextAttempt > _settings.MaxAttempts)
        {
            return false;
        }

        var delay = CalculateDelay(nextAttempt);
        var retryEvent = new RefundRequestedEvent
        {
            CorrelationId = failedMessage.CorrelationId,
            RefundRequestId = failedMessage.RefundRequestId,
            ReturnRequestId = failedMessage.ReturnRequestId,
            OrderId = failedMessage.OrderId,
            UserId = failedMessage.UserId,
            Amount = failedMessage.Amount,
            Currency = failedMessage.Currency,
            RetryAttempt = nextAttempt
        };

        _backgroundJobClient.Create(
            Job.FromExpression<IRefundRetryJob>(job => job.PublishRetryAsync(retryEvent)),
            new ScheduledState(delay));

        _logger.LogWarning(
            "Refund retry scheduled. RefundRequestId={RefundRequestId}, OrderId={OrderId}, RetryAttempt={RetryAttempt}, DelayMinutes={DelayMinutes}",
            failedMessage.RefundRequestId,
            failedMessage.OrderId,
            nextAttempt,
            delay.TotalMinutes);

        return true;
    }

    private TimeSpan CalculateDelay(int attempt)
    {
        var delayMinutes = _settings.InitialDelayMinutes * Math.Pow(_settings.BackoffMultiplier, Math.Max(0, attempt - 1));
        return TimeSpan.FromMinutes(delayMinutes);
    }
}
