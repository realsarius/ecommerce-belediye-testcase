using EcommerceAPI.Application.Abstractions.ServiceContracts;
using EcommerceAPI.Entities.IntegrationEvents;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace EcommerceAPI.Infrastructure.Services;

public class RefundRetryJob : IRefundRetryJob
{
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<RefundRetryJob> _logger;

    public RefundRetryJob(
        IPublishEndpoint publishEndpoint,
        ILogger<RefundRetryJob> logger)
    {
        _publishEndpoint = publishEndpoint;
        _logger = logger;
    }

    public async Task PublishRetryAsync(RefundRequestedEvent message)
    {
        await _publishEndpoint.Publish(message);

        _logger.LogInformation(
            "Refund retry event published. RefundRequestId={RefundRequestId}, OrderId={OrderId}, RetryAttempt={RetryAttempt}",
            message.RefundRequestId,
            message.OrderId,
            message.RetryAttempt);
    }
}
