using EcommerceAPI.Entities.IntegrationEvents;
using EcommerceAPI.DataAccess.Concrete.EntityFramework.Contexts;
using EcommerceAPI.Entities.Concrete;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace EcommerceAPI.API.Consumers;

public sealed class OrderCreatedConsumer : IConsumer<OrderCreatedEvent>
{
    private const string ConsumerName = nameof(OrderCreatedConsumer);
    private readonly AppDbContext _dbContext;
    private readonly ILogger<OrderCreatedConsumer> _logger;

    public OrderCreatedConsumer(
        AppDbContext dbContext,
        ILogger<OrderCreatedConsumer> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<OrderCreatedEvent> context)
    {
        var message = context.Message;
        var messageId = context.MessageId ?? message.EventId;
        var activity = Activity.Current;

        if (activity is not null)
        {
            activity.SetTag("ecommerce.messaging.consumer", ConsumerName);
            activity.SetTag("ecommerce.message.type", nameof(OrderCreatedEvent));
            activity.SetTag("ecommerce.order.id", message.OrderId);
            activity.SetTag("ecommerce.order.number", message.OrderNumber);
        }

        var alreadyProcessed = await _dbContext.InboxMessages
            .AnyAsync(
                x => x.ConsumerName == ConsumerName && x.MessageId == messageId,
                context.CancellationToken);

        if (alreadyProcessed)
        {
            _logger.LogInformation(
                "OrderCreatedEvent duplicate skipped. OrderId={OrderId}, MessageId={MessageId}",
                message.OrderId,
                messageId);
            return;
        }

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
            messageId);

        _dbContext.InboxMessages.Add(new InboxMessage
        {
            MessageId = messageId,
            ConsumerName = ConsumerName,
            MessageType = typeof(OrderCreatedEvent).FullName ?? nameof(OrderCreatedEvent),
            ProcessedOnUtc = DateTime.UtcNow
        });

        try
        {
            await _dbContext.SaveChangesAsync(context.CancellationToken);
        }
        catch (DbUpdateException ex) when (IsDuplicateKeyException(ex))
        {
            _logger.LogInformation(
                "OrderCreatedEvent duplicate detected during inbox save. OrderId={OrderId}, MessageId={MessageId}",
                message.OrderId,
                messageId);
        }
    }

    private static bool IsDuplicateKeyException(DbUpdateException ex)
    {
        return ex.InnerException?.Message.Contains("duplicate key", StringComparison.OrdinalIgnoreCase) == true;
    }
}
