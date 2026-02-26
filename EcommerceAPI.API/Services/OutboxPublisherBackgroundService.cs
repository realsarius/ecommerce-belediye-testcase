using System.Text.Json;
using EcommerceAPI.DataAccess.Concrete.EntityFramework.Contexts;
using EcommerceAPI.Entities.Concrete;
using EcommerceAPI.Entities.IntegrationEvents;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace EcommerceAPI.API.Services;

public sealed class OutboxPublisherBackgroundService : BackgroundService
{
    private const int BatchSize = 50;
    private const int MaxRetryCount = 10;
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(5);
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OutboxPublisherBackgroundService> _logger;

    public OutboxPublisherBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<OutboxPublisherBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Outbox publisher background service started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var publishEndpoint = scope.ServiceProvider.GetRequiredService<IPublishEndpoint>();

                var pendingMessages = await dbContext.OutboxMessages
                    .Where(x => x.ProcessedOnUtc == null && x.RetryCount < MaxRetryCount)
                    .OrderBy(x => x.CreatedAt)
                    .Take(BatchSize)
                    .ToListAsync(stoppingToken);

                if (pendingMessages.Count == 0)
                {
                    await Task.Delay(PollInterval, stoppingToken);
                    continue;
                }

                foreach (var outboxMessage in pendingMessages)
                {
                    await ProcessMessageAsync(outboxMessage, publishEndpoint, stoppingToken);
                }

                await dbContext.SaveChangesAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Outbox publisher loop failed.");
                await Task.Delay(PollInterval, stoppingToken);
            }
        }

        _logger.LogInformation("Outbox publisher background service stopped.");
    }

    private async Task ProcessMessageAsync(
        OutboxMessage outboxMessage,
        IPublishEndpoint publishEndpoint,
        CancellationToken cancellationToken)
    {
        try
        {
            switch (outboxMessage.EventType)
            {
                case var eventType when eventType == (typeof(OrderCreatedEvent).FullName ?? nameof(OrderCreatedEvent)):
                    var orderCreated = JsonSerializer.Deserialize<OrderCreatedEvent>(outboxMessage.Payload, SerializerOptions);
                    if (orderCreated is null)
                        throw new InvalidOperationException("OrderCreatedEvent deserialization failed.");
                    await publishEndpoint.Publish(orderCreated, cancellationToken);
                    break;

                case var eventType when eventType == (typeof(ProductIndexSyncEvent).FullName ?? nameof(ProductIndexSyncEvent)):
                    var productIndexSync = JsonSerializer.Deserialize<ProductIndexSyncEvent>(outboxMessage.Payload, SerializerOptions);
                    if (productIndexSync is null)
                        throw new InvalidOperationException("ProductIndexSyncEvent deserialization failed.");
                    await publishEndpoint.Publish(productIndexSync, cancellationToken);
                    break;

                default:
                    throw new InvalidOperationException($"Unsupported outbox event type: {outboxMessage.EventType}");
            }

            outboxMessage.ProcessedOnUtc = DateTime.UtcNow;
            outboxMessage.LastError = null;
            outboxMessage.UpdatedAt = DateTime.UtcNow;

            _logger.LogInformation(
                "Outbox message published. EventType={EventType}, EventId={EventId}, OutboxId={OutboxId}",
                outboxMessage.EventType,
                outboxMessage.EventId,
                outboxMessage.Id);
        }
        catch (Exception ex)
        {
            outboxMessage.RetryCount += 1;
            outboxMessage.LastError = ex.Message.Length > 2000 ? ex.Message[..2000] : ex.Message;
            outboxMessage.UpdatedAt = DateTime.UtcNow;

            _logger.LogWarning(
                ex,
                "Outbox publish failed. EventType={EventType}, EventId={EventId}, RetryCount={RetryCount}",
                outboxMessage.EventType,
                outboxMessage.EventId,
                outboxMessage.RetryCount);
        }
    }
}
