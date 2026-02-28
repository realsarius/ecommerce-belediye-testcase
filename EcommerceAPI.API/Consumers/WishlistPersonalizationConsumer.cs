using System.Diagnostics;
using EcommerceAPI.DataAccess.Concrete.EntityFramework.Contexts;
using EcommerceAPI.Entities.Concrete;
using EcommerceAPI.Entities.IntegrationEvents;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;

namespace EcommerceAPI.API.Consumers;

public sealed class WishlistPersonalizationConsumer :
    IConsumer<WishlistItemAddedEvent>,
    IConsumer<WishlistItemRemovedEvent>
{
    private const string ConsumerName = nameof(WishlistPersonalizationConsumer);
    private readonly AppDbContext _dbContext;
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<WishlistPersonalizationConsumer> _logger;

    public WishlistPersonalizationConsumer(
        AppDbContext dbContext,
        IConnectionMultiplexer redis,
        ILogger<WishlistPersonalizationConsumer> logger)
    {
        _dbContext = dbContext;
        _redis = redis;
        _logger = logger;
    }

    public Task Consume(ConsumeContext<WishlistItemAddedEvent> context)
    {
        return ConsumeAsync(
            context,
            nameof(WishlistItemAddedEvent),
            context.Message.EventId,
            context.Message.UserId,
            context.Message.ProductId,
            1);
    }

    public Task Consume(ConsumeContext<WishlistItemRemovedEvent> context)
    {
        return ConsumeAsync(
            context,
            nameof(WishlistItemRemovedEvent),
            context.Message.EventId,
            context.Message.UserId,
            context.Message.ProductId,
            -1);
    }

    private async Task ConsumeAsync<TMessage>(
        ConsumeContext<TMessage> context,
        string messageType,
        Guid fallbackMessageId,
        int userId,
        int productId,
        int delta)
        where TMessage : class
    {
        var messageId = context.MessageId ?? fallbackMessageId;
        AddActivityTags(messageType, userId, productId);

        var alreadyProcessed = await _dbContext.InboxMessages.AnyAsync(
            x => x.ConsumerName == ConsumerName && x.MessageId == messageId,
            context.CancellationToken);

        if (alreadyProcessed)
        {
            _logger.LogInformation(
                "{MessageType} duplicate skipped for personalization. UserId={UserId}, ProductId={ProductId}, MessageId={MessageId}",
                messageType,
                userId,
                productId,
                messageId);
            return;
        }

        var productInfo = await _dbContext.Products
            .Where(p => p.Id == productId)
            .Select(p => new
            {
                p.CategoryId,
                CategoryName = p.Category != null ? p.Category.Name : null
            })
            .FirstOrDefaultAsync(context.CancellationToken);

        if (productInfo != null)
        {
            var db = _redis.GetDatabase();
            var key = $"wishlist:preferences:user:{userId}";
            var field = productInfo.CategoryId.ToString();

            var score = await db.HashIncrementAsync(key, field, delta);
            if (score <= 0)
            {
                await db.HashDeleteAsync(key, field);
                score = 0;
            }

            _logger.LogInformation(
                "Wishlist personalization updated. EventType={EventType}, UserId={UserId}, ProductId={ProductId}, CategoryId={CategoryId}, Category={Category}, Score={Score}, MessageId={MessageId}",
                messageType,
                userId,
                productId,
                productInfo.CategoryId,
                productInfo.CategoryName,
                score,
                messageId);
        }
        else
        {
            _logger.LogInformation(
                "Wishlist personalization skipped because product metadata was not found. EventType={EventType}, UserId={UserId}, ProductId={ProductId}, MessageId={MessageId}",
                messageType,
                userId,
                productId,
                messageId);
        }

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
                "{MessageType} duplicate detected during personalization inbox save. UserId={UserId}, ProductId={ProductId}, MessageId={MessageId}",
                messageType,
                userId,
                productId,
                messageId);
        }
    }

    private static void AddActivityTags(string messageType, int userId, int productId)
    {
        var activity = Activity.Current;
        if (activity is null)
        {
            return;
        }

        activity.SetTag("ecommerce.messaging.consumer", ConsumerName);
        activity.SetTag("ecommerce.message.type", messageType);
        activity.SetTag("ecommerce.user.id", userId);
        activity.SetTag("ecommerce.product.id", productId);
    }

    private static bool IsDuplicateKeyException(DbUpdateException ex)
    {
        return ex.InnerException?.Message.Contains("duplicate key", StringComparison.OrdinalIgnoreCase) == true;
    }
}
