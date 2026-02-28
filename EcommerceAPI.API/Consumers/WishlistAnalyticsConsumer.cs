using System.Diagnostics;
using EcommerceAPI.DataAccess.Concrete.EntityFramework.Contexts;
using EcommerceAPI.Entities.Concrete;
using EcommerceAPI.Entities.IntegrationEvents;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace EcommerceAPI.API.Consumers;

public sealed class WishlistAnalyticsConsumer :
    IConsumer<WishlistItemAddedEvent>,
    IConsumer<WishlistItemRemovedEvent>
{
    private const string ConsumerName = nameof(WishlistAnalyticsConsumer);
    private readonly AppDbContext _dbContext;
    private readonly ILogger<WishlistAnalyticsConsumer> _logger;

    public WishlistAnalyticsConsumer(
        AppDbContext dbContext,
        ILogger<WishlistAnalyticsConsumer> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public Task Consume(ConsumeContext<WishlistItemAddedEvent> context)
    {
        return ConsumeAddedAsync(context);
    }

    public Task Consume(ConsumeContext<WishlistItemRemovedEvent> context)
    {
        return ConsumeRemovedAsync(context);
    }

    private async Task ConsumeAddedAsync(ConsumeContext<WishlistItemAddedEvent> context)
    {
        var message = context.Message;
        var messageId = context.MessageId ?? message.EventId;
        AddActivityTags(nameof(WishlistItemAddedEvent), message.ProductId, message.UserId);

        if (await IsAlreadyProcessedAsync(messageId, context.CancellationToken))
        {
            _logger.LogInformation(
                "WishlistItemAddedEvent duplicate skipped. ProductId={ProductId}, UserId={UserId}, MessageId={MessageId}",
                message.ProductId,
                message.UserId,
                messageId);
            return;
        }

        var productInfo = await _dbContext.Products
            .Where(p => p.Id == message.ProductId)
            .Select(p => new
            {
                Category = p.Category != null ? p.Category.Name : null,
                p.WishlistCount,
                p.IsActive
            })
            .FirstOrDefaultAsync(context.CancellationToken);

        _logger.LogInformation(
            "Wishlist analytics event processed. EventType={EventType}, UserId={UserId}, WishlistId={WishlistId}, ProductId={ProductId}, Category={Category}, PriceAtTime={PriceAtTime}, Currency={Currency}, WishlistCount={WishlistCount}, IsActive={IsActive}, MessageId={MessageId}",
            nameof(WishlistItemAddedEvent),
            message.UserId,
            message.WishlistId,
            message.ProductId,
            productInfo?.Category,
            message.PriceAtTime,
            message.Currency,
            productInfo?.WishlistCount,
            productInfo?.IsActive,
            messageId);

        await SaveInboxMessageAsync(messageId, typeof(WishlistItemAddedEvent), context.CancellationToken);
    }

    private async Task ConsumeRemovedAsync(ConsumeContext<WishlistItemRemovedEvent> context)
    {
        var message = context.Message;
        var messageId = context.MessageId ?? message.EventId;
        AddActivityTags(nameof(WishlistItemRemovedEvent), message.ProductId, message.UserId);

        if (await IsAlreadyProcessedAsync(messageId, context.CancellationToken))
        {
            _logger.LogInformation(
                "WishlistItemRemovedEvent duplicate skipped. ProductId={ProductId}, UserId={UserId}, MessageId={MessageId}",
                message.ProductId,
                message.UserId,
                messageId);
            return;
        }

        var productInfo = await _dbContext.Products
            .Where(p => p.Id == message.ProductId)
            .Select(p => new
            {
                Category = p.Category != null ? p.Category.Name : null,
                p.WishlistCount,
                p.IsActive
            })
            .FirstOrDefaultAsync(context.CancellationToken);

        _logger.LogInformation(
            "Wishlist analytics event processed. EventType={EventType}, UserId={UserId}, WishlistId={WishlistId}, ProductId={ProductId}, Category={Category}, Reason={Reason}, WishlistCount={WishlistCount}, IsActive={IsActive}, MessageId={MessageId}",
            nameof(WishlistItemRemovedEvent),
            message.UserId,
            message.WishlistId,
            message.ProductId,
            productInfo?.Category,
            message.Reason,
            productInfo?.WishlistCount,
            productInfo?.IsActive,
            messageId);

        await SaveInboxMessageAsync(messageId, typeof(WishlistItemRemovedEvent), context.CancellationToken);
    }

    private static void AddActivityTags(string messageType, int productId, int userId)
    {
        var activity = Activity.Current;
        if (activity is null)
        {
            return;
        }

        activity.SetTag("ecommerce.messaging.consumer", ConsumerName);
        activity.SetTag("ecommerce.message.type", messageType);
        activity.SetTag("ecommerce.product.id", productId);
        activity.SetTag("ecommerce.user.id", userId);
    }

    private Task<bool> IsAlreadyProcessedAsync(Guid messageId, CancellationToken cancellationToken)
    {
        return _dbContext.InboxMessages.AnyAsync(
            x => x.ConsumerName == ConsumerName && x.MessageId == messageId,
            cancellationToken);
    }

    private async Task SaveInboxMessageAsync(Guid messageId, Type messageType, CancellationToken cancellationToken)
    {
        _dbContext.InboxMessages.Add(new InboxMessage
        {
            MessageId = messageId,
            ConsumerName = ConsumerName,
            MessageType = messageType.FullName ?? messageType.Name,
            ProcessedOnUtc = DateTime.UtcNow
        });

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (IsDuplicateKeyException(ex))
        {
            _logger.LogInformation(
                "Wishlist analytics consumer duplicate detected during inbox save. Consumer={ConsumerName}, MessageId={MessageId}",
                ConsumerName,
                messageId);
        }
    }

    private static bool IsDuplicateKeyException(DbUpdateException ex)
    {
        return ex.InnerException?.Message.Contains("duplicate key", StringComparison.OrdinalIgnoreCase) == true;
    }
}
