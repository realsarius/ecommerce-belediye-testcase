using EcommerceAPI.Business.Abstract;
using EcommerceAPI.Entities.IntegrationEvents;
using EcommerceAPI.DataAccess.Concrete.EntityFramework.Contexts;
using EcommerceAPI.Entities.Concrete;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace EcommerceAPI.API.Consumers;

public sealed class ProductIndexSyncConsumer : IConsumer<ProductIndexSyncEvent>
{
    private const string ConsumerName = nameof(ProductIndexSyncConsumer);
    private readonly AppDbContext _dbContext;
    private readonly IProductSearchIndexService _productSearchIndexService;
    private readonly ILogger<ProductIndexSyncConsumer> _logger;

    public ProductIndexSyncConsumer(
        AppDbContext dbContext,
        IProductSearchIndexService productSearchIndexService,
        ILogger<ProductIndexSyncConsumer> logger)
    {
        _dbContext = dbContext;
        _productSearchIndexService = productSearchIndexService;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<ProductIndexSyncEvent> context)
    {
        var message = context.Message;
        var messageId = context.MessageId ?? message.EventId;
        var forceFailByEnv = string.Equals(
            Environment.GetEnvironmentVariable("PRODUCT_INDEX_SYNC_FORCE_FAIL"),
            "true",
            StringComparison.OrdinalIgnoreCase);
        var forceFailByReason = !string.IsNullOrWhiteSpace(message.Reason) &&
                                message.Reason.StartsWith("dlq-test-", StringComparison.OrdinalIgnoreCase);

        var alreadyProcessed = await _dbContext.InboxMessages
            .AnyAsync(
                x => x.ConsumerName == ConsumerName && x.MessageId == messageId,
                context.CancellationToken);

        if (alreadyProcessed)
        {
            _logger.LogInformation(
                "ProductIndexSyncEvent duplicate skipped. ProductId={ProductId}, MessageId={MessageId}",
                message.ProductId,
                messageId);
            return;
        }

        _logger.LogInformation(
            "ProductIndexSyncEvent received. ProductId={ProductId}, Operation={Operation}, Reason={Reason}, MessageId={MessageId}",
            message.ProductId,
            message.Operation,
            message.Reason,
            messageId);

        if (forceFailByEnv || forceFailByReason)
        {
            _logger.LogWarning(
                "ProductIndexSyncEvent forced failure for retry/DLQ test. ProductId={ProductId}, Operation={Operation}, Reason={Reason}, MessageId={MessageId}",
                message.ProductId,
                message.Operation,
                message.Reason,
                context.MessageId);

            throw new InvalidOperationException("Forced failure for ProductIndexSync retry/DLQ validation");
        }

        if (string.Equals(message.Operation, ProductIndexOperations.Delete, StringComparison.OrdinalIgnoreCase))
        {
            await _productSearchIndexService.DeleteProductAsync(message.ProductId, context.CancellationToken);
        }
        else
        {
            await _productSearchIndexService.IndexProductAsync(message.ProductId, context.CancellationToken);
        }

        _logger.LogInformation(
            "ProductIndexSyncEvent completed. ProductId={ProductId}, Operation={Operation}",
            message.ProductId,
            message.Operation);

        _dbContext.InboxMessages.Add(new InboxMessage
        {
            MessageId = messageId,
            ConsumerName = ConsumerName,
            MessageType = typeof(ProductIndexSyncEvent).FullName ?? nameof(ProductIndexSyncEvent),
            ProcessedOnUtc = DateTime.UtcNow
        });

        try
        {
            await _dbContext.SaveChangesAsync(context.CancellationToken);
        }
        catch (DbUpdateException ex) when (IsDuplicateKeyException(ex))
        {
            _logger.LogInformation(
                "ProductIndexSyncEvent duplicate detected during inbox save. ProductId={ProductId}, MessageId={MessageId}",
                message.ProductId,
                messageId);
        }
    }

    private static bool IsDuplicateKeyException(DbUpdateException ex)
    {
        return ex.InnerException?.Message.Contains("duplicate key", StringComparison.OrdinalIgnoreCase) == true;
    }
}
