using EcommerceAPI.Business.Abstract;
using EcommerceAPI.Entities.IntegrationEvents;
using MassTransit;

namespace EcommerceAPI.API.Consumers;

public sealed class ProductIndexSyncConsumer : IConsumer<ProductIndexSyncEvent>
{
    private readonly IProductSearchIndexService _productSearchIndexService;
    private readonly ILogger<ProductIndexSyncConsumer> _logger;

    public ProductIndexSyncConsumer(
        IProductSearchIndexService productSearchIndexService,
        ILogger<ProductIndexSyncConsumer> logger)
    {
        _productSearchIndexService = productSearchIndexService;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<ProductIndexSyncEvent> context)
    {
        var message = context.Message;
        var forceFailByEnv = string.Equals(
            Environment.GetEnvironmentVariable("PRODUCT_INDEX_SYNC_FORCE_FAIL"),
            "true",
            StringComparison.OrdinalIgnoreCase);
        var forceFailByReason = !string.IsNullOrWhiteSpace(message.Reason) &&
                                message.Reason.StartsWith("dlq-test-", StringComparison.OrdinalIgnoreCase);

        _logger.LogInformation(
            "ProductIndexSyncEvent received. ProductId={ProductId}, Operation={Operation}, Reason={Reason}, MessageId={MessageId}",
            message.ProductId,
            message.Operation,
            message.Reason,
            context.MessageId);

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
    }
}
