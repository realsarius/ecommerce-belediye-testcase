using EcommerceAPI.Entities.IntegrationEvents;
using MassTransit;

namespace EcommerceAPI.API.Consumers;

public sealed class OrderCreatedConsumer : IConsumer<OrderCreatedEvent>
{
    private readonly ILogger<OrderCreatedConsumer> _logger;

    public OrderCreatedConsumer(ILogger<OrderCreatedConsumer> logger)
    {
        _logger = logger;
    }

    public Task Consume(ConsumeContext<OrderCreatedEvent> context)
    {
        var message = context.Message;

        if (!string.IsNullOrWhiteSpace(message.IdempotencyKey) &&
            message.IdempotencyKey.StartsWith("dlq-test-", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning(
                "OrderCreatedEvent forced failure for retry/DLQ test. OrderId={OrderId}, OrderNumber={OrderNumber}, IdempotencyKey={IdempotencyKey}",
                message.OrderId,
                message.OrderNumber,
                message.IdempotencyKey);

            throw new InvalidOperationException("Forced failure for retry/DLQ validation");
        }

        _logger.LogInformation(
            "OrderCreatedEvent consumed. OrderId={OrderId}, OrderNumber={OrderNumber}, MessageId={MessageId}",
            message.OrderId,
            message.OrderNumber,
            context.MessageId);

        return Task.CompletedTask;
    }
}
