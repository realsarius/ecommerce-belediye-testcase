using System.Diagnostics;
using EcommerceAPI.API.Hubs;
using EcommerceAPI.DataAccess.Concrete.EntityFramework.Contexts;
using EcommerceAPI.Entities.Concrete;
using EcommerceAPI.Entities.IntegrationEvents;
using MassTransit;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace EcommerceAPI.API.Consumers;

public sealed class WishlistPriceAlertNotificationConsumer : IConsumer<WishlistProductPriceDropEvent>
{
    private const string ConsumerName = nameof(WishlistPriceAlertNotificationConsumer);
    private readonly AppDbContext _dbContext;
    private readonly IHubContext<WishlistHub> _hubContext;
    private readonly ILogger<WishlistPriceAlertNotificationConsumer> _logger;

    public WishlistPriceAlertNotificationConsumer(
        AppDbContext dbContext,
        IHubContext<WishlistHub> hubContext,
        ILogger<WishlistPriceAlertNotificationConsumer> logger)
    {
        _dbContext = dbContext;
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<WishlistProductPriceDropEvent> context)
    {
        var message = context.Message;
        var messageId = context.MessageId ?? message.EventId;
        AddActivityTags(message);

        var alreadyProcessed = await _dbContext.InboxMessages.AnyAsync(
            x => x.ConsumerName == ConsumerName && x.MessageId == messageId,
            context.CancellationToken);

        if (alreadyProcessed)
        {
            _logger.LogInformation(
                "WishlistProductPriceDropEvent duplicate skipped. UserId={UserId}, ProductId={ProductId}, MessageId={MessageId}",
                message.UserId,
                message.ProductId,
                messageId);
            return;
        }

        var productInfo = await _dbContext.Products
            .AsNoTracking()
            .Where(p => p.Id == message.ProductId)
            .Select(p => new
            {
                Category = p.Category != null ? p.Category.Name : null,
                p.IsActive
            })
            .FirstOrDefaultAsync(context.CancellationToken);

        await _hubContext.Clients.Group(WishlistHub.UserGroup(message.UserId))
            .SendAsync(
                "PriceAlertTriggered",
                new
                {
                    message.ProductId,
                    message.ProductName,
                    message.TargetPrice,
                    message.OldPrice,
                    message.NewPrice,
                    message.Currency,
                    message.OccurredAt
                },
                context.CancellationToken);

        _logger.LogInformation(
            "Wishlist analytics event. AnalyticsStream={AnalyticsStream}, AnalyticsEvent={AnalyticsEvent}, NotificationChannel={NotificationChannel}, UserId={UserId}, ProductId={ProductId}, ProductName={ProductName}, Category={Category}, OldPrice={OldPrice}, NewPrice={NewPrice}, TargetPrice={TargetPrice}, Currency={Currency}, IsActive={IsActive}, MessageId={MessageId}, OccurredAt={OccurredAt}",
            "Wishlist",
            "WishlistPriceAlertDelivered",
            "SignalR",
            message.UserId,
            message.ProductId,
            message.ProductName,
            productInfo?.Category,
            message.OldPrice,
            message.NewPrice,
            message.TargetPrice,
            message.Currency,
            productInfo?.IsActive,
            messageId,
            message.OccurredAt);

        _dbContext.InboxMessages.Add(new InboxMessage
        {
            MessageId = messageId,
            ConsumerName = ConsumerName,
            MessageType = typeof(WishlistProductPriceDropEvent).FullName ?? nameof(WishlistProductPriceDropEvent),
            ProcessedOnUtc = DateTime.UtcNow
        });

        try
        {
            await _dbContext.SaveChangesAsync(context.CancellationToken);
        }
        catch (DbUpdateException ex) when (IsDuplicateKeyException(ex))
        {
            _logger.LogInformation(
                "WishlistProductPriceDropEvent duplicate detected during inbox save. UserId={UserId}, ProductId={ProductId}, MessageId={MessageId}",
                message.UserId,
                message.ProductId,
                messageId);
        }
    }

    private static void AddActivityTags(WishlistProductPriceDropEvent message)
    {
        var activity = Activity.Current;
        if (activity is null)
        {
            return;
        }

        activity.SetTag("ecommerce.messaging.consumer", ConsumerName);
        activity.SetTag("ecommerce.message.type", nameof(WishlistProductPriceDropEvent));
        activity.SetTag("ecommerce.user.id", message.UserId);
        activity.SetTag("ecommerce.product.id", message.ProductId);
    }

    private static bool IsDuplicateKeyException(DbUpdateException ex)
    {
        return ex.InnerException?.Message.Contains("duplicate key", StringComparison.OrdinalIgnoreCase) == true;
    }
}
