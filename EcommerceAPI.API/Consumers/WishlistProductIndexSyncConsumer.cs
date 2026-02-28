using System.Diagnostics;
using EcommerceAPI.Business.Abstract;
using EcommerceAPI.DataAccess.Concrete.EntityFramework.Contexts;
using EcommerceAPI.Entities.Concrete;
using EcommerceAPI.Entities.IntegrationEvents;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace EcommerceAPI.API.Consumers;

public sealed class WishlistProductIndexSyncConsumer :
    IConsumer<WishlistItemAddedEvent>,
    IConsumer<WishlistItemRemovedEvent>
{
    private const string ConsumerName = nameof(WishlistProductIndexSyncConsumer);
    private readonly AppDbContext _dbContext;
    private readonly IProductSearchIndexService _productSearchIndexService;
    private readonly ILogger<WishlistProductIndexSyncConsumer> _logger;

    public WishlistProductIndexSyncConsumer(
        AppDbContext dbContext,
        IProductSearchIndexService productSearchIndexService,
        ILogger<WishlistProductIndexSyncConsumer> logger)
    {
        _dbContext = dbContext;
        _productSearchIndexService = productSearchIndexService;
        _logger = logger;
    }

    public Task Consume(ConsumeContext<WishlistItemAddedEvent> context)
    {
        return ConsumeAsync(context, nameof(WishlistItemAddedEvent), context.Message.EventId, context.Message.ProductId);
    }

    public Task Consume(ConsumeContext<WishlistItemRemovedEvent> context)
    {
        return ConsumeAsync(context, nameof(WishlistItemRemovedEvent), context.Message.EventId, context.Message.ProductId);
    }

    private async Task ConsumeAsync<TMessage>(
        ConsumeContext<TMessage> context,
        string messageType,
        Guid fallbackMessageId,
        int productId)
        where TMessage : class
    {
        var messageId = context.MessageId ?? fallbackMessageId;
        AddActivityTags(messageType, productId);

        var alreadyProcessed = await _dbContext.InboxMessages.AnyAsync(
            x => x.ConsumerName == ConsumerName && x.MessageId == messageId,
            context.CancellationToken);

        if (alreadyProcessed)
        {
            _logger.LogInformation(
                "{MessageType} duplicate skipped for search index sync. ProductId={ProductId}, MessageId={MessageId}",
                messageType,
                productId,
                messageId);
            return;
        }

        await _productSearchIndexService.IndexProductAsync(productId, context.CancellationToken);

        _logger.LogInformation(
            "{MessageType} processed for product search index sync. ProductId={ProductId}, MessageId={MessageId}",
            messageType,
            productId,
            messageId);

        _dbContext.InboxMessages.Add(new InboxMessage
        {
            MessageId = messageId,
            ConsumerName = ConsumerName,
            MessageType = typeof(TMessage).FullName ?? messageType,
            ProcessedOnUtc = DateTime.UtcNow
        });

        try
        {
            await _dbContext.SaveChangesAsync(context.CancellationToken);
        }
        catch (DbUpdateException ex) when (IsDuplicateKeyException(ex))
        {
            _logger.LogInformation(
                "{MessageType} duplicate detected during inbox save. ProductId={ProductId}, MessageId={MessageId}",
                messageType,
                productId,
                messageId);
        }
    }

    private static void AddActivityTags(string messageType, int productId)
    {
        var activity = Activity.Current;
        if (activity is null)
        {
            return;
        }

        activity.SetTag("ecommerce.messaging.consumer", ConsumerName);
        activity.SetTag("ecommerce.message.type", messageType);
        activity.SetTag("ecommerce.product.id", productId);
    }

    private static bool IsDuplicateKeyException(DbUpdateException ex)
    {
        return ex.InnerException?.Message.Contains("duplicate key", StringComparison.OrdinalIgnoreCase) == true;
    }
}
