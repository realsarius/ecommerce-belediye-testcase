using System.Diagnostics;
using EcommerceAPI.API.Hubs;
using EcommerceAPI.Business.Abstract;
using EcommerceAPI.Core.Interfaces;
using EcommerceAPI.DataAccess.Concrete.EntityFramework.Contexts;
using EcommerceAPI.Entities.Concrete;
using EcommerceAPI.Entities.IntegrationEvents;
using MassTransit;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace EcommerceAPI.API.Consumers;

public sealed class WishlistLowStockNotificationConsumer : IConsumer<WishlistProductLowStockEvent>
{
    private const string ConsumerName = nameof(WishlistLowStockNotificationConsumer);
    private readonly AppDbContext _dbContext;
    private readonly IEmailNotificationService _emailNotificationService;
    private readonly IHubContext<WishlistHub> _hubContext;
    private readonly INotificationService _notificationService;
    private readonly ILogger<WishlistLowStockNotificationConsumer> _logger;

    public WishlistLowStockNotificationConsumer(
        AppDbContext dbContext,
        IEmailNotificationService emailNotificationService,
        IHubContext<WishlistHub> hubContext,
        INotificationService notificationService,
        ILogger<WishlistLowStockNotificationConsumer> logger)
    {
        _dbContext = dbContext;
        _emailNotificationService = emailNotificationService;
        _hubContext = hubContext;
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<WishlistProductLowStockEvent> context)
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
                "WishlistProductLowStockEvent duplicate skipped. ProductId={ProductId}, MessageId={MessageId}",
                message.ProductId,
                messageId);
            return;
        }

        var userIds = await _dbContext.WishlistItems
            .AsNoTracking()
            .Where(item => item.ProductId == message.ProductId)
            .Join(
                _dbContext.Wishlists.AsNoTracking(),
                item => item.WishlistId,
                wishlist => wishlist.Id,
                (item, wishlist) => wishlist.UserId)
            .Distinct()
            .ToListAsync(context.CancellationToken);

        var users = await _dbContext.Users
            .AsNoTracking()
            .Where(u => userIds.Contains(u.Id))
            .Select(u => new
            {
                u.Id,
                u.Email,
                u.FirstName,
                u.LastName
            })
            .ToListAsync(context.CancellationToken);

        var product = await _dbContext.Products
            .AsNoTracking()
            .Where(p => p.Id == message.ProductId)
            .Select(p => new
            {
                p.Name,
                Category = p.Category != null ? p.Category.Name : null,
                p.IsActive
            })
            .FirstOrDefaultAsync(context.CancellationToken);

        if (product?.IsActive == false)
        {
            _logger.LogInformation(
                "Wishlist analytics event. AnalyticsStream={AnalyticsStream}, AnalyticsEvent={AnalyticsEvent}, ProductId={ProductId}, StockQuantity={StockQuantity}, Threshold={Threshold}, Reason={Reason}, NotifiedUsers={UserCount}, IsActive={IsActive}, MessageId={MessageId}, OccurredAt={OccurredAt}",
                "Wishlist",
                "WishlistLowStockSkipped",
                message.ProductId,
                message.StockQuantity,
                message.Threshold,
                message.Reason,
                userIds.Count,
                false,
                messageId,
                message.OccurredAt);
        }
        else if (userIds.Count > 0)
        {
            var productName = string.IsNullOrWhiteSpace(product?.Name)
                ? $"Ürün #{message.ProductId}"
                : product.Name;

            foreach (var userId in userIds)
            {
                await _hubContext.Clients.Group(WishlistHub.UserGroup(userId))
                    .SendAsync(
                        "LowStockAlertTriggered",
                        new
                        {
                            message.ProductId,
                            ProductName = productName,
                            message.StockQuantity,
                            message.Threshold,
                            message.OccurredAt
                        },
                        context.CancellationToken);

                await _notificationService.CreateNotificationAsync(new Entities.DTOs.CreateNotificationRequest
                {
                    UserId = userId,
                    Type = "Wishlist",
                    Title = $"{productName} stokta azalıyor",
                    Body = $"Kalan stok: {message.StockQuantity}. Ürünü kaçırmamak için göz atın.",
                    DeepLink = $"/products/{message.ProductId}"
                });
            }

            _logger.LogInformation(
                "Wishlist analytics event. AnalyticsStream={AnalyticsStream}, AnalyticsEvent={AnalyticsEvent}, NotificationChannel={NotificationChannel}, ProductId={ProductId}, ProductName={ProductName}, Category={Category}, StockQuantity={StockQuantity}, Threshold={Threshold}, Reason={Reason}, NotifiedUsers={UserCount}, IsActive={IsActive}, MessageId={MessageId}, OccurredAt={OccurredAt}",
                "Wishlist",
                "WishlistLowStockDelivered",
                "SignalR",
                message.ProductId,
                productName,
                product?.Category,
                message.StockQuantity,
                message.Threshold,
                message.Reason,
                userIds.Count,
                product?.IsActive,
                messageId,
                message.OccurredAt);

            var emailedUsers = 0;
            foreach (var user in users)
            {
                var emailSent = await _emailNotificationService.SendAsync(
                    user.Email,
                    $"{productName} için stok azalıyor",
                    BuildLowStockEmailBody(
                        string.Join(' ', new[] { user.FirstName, user.LastName }.Where(x => !string.IsNullOrWhiteSpace(x))),
                        productName,
                        message),
                    context.CancellationToken);

                if (emailSent)
                {
                    emailedUsers++;
                }
            }

            if (emailedUsers > 0)
            {
                _logger.LogInformation(
                    "Wishlist analytics event. AnalyticsStream={AnalyticsStream}, AnalyticsEvent={AnalyticsEvent}, NotificationChannel={NotificationChannel}, ProductId={ProductId}, ProductName={ProductName}, Category={Category}, StockQuantity={StockQuantity}, Threshold={Threshold}, Reason={Reason}, NotifiedUsers={UserCount}, IsActive={IsActive}, MessageId={MessageId}, OccurredAt={OccurredAt}",
                    "Wishlist",
                    "WishlistLowStockDelivered",
                    "Email",
                    message.ProductId,
                    productName,
                    product?.Category,
                    message.StockQuantity,
                    message.Threshold,
                    message.Reason,
                    emailedUsers,
                    product?.IsActive,
                    messageId,
                    message.OccurredAt);
            }
        }

        _dbContext.InboxMessages.Add(new InboxMessage
        {
            MessageId = messageId,
            ConsumerName = ConsumerName,
            MessageType = typeof(WishlistProductLowStockEvent).FullName ?? nameof(WishlistProductLowStockEvent),
            ProcessedOnUtc = DateTime.UtcNow
        });

        try
        {
            await _dbContext.SaveChangesAsync(context.CancellationToken);
        }
        catch (DbUpdateException ex) when (IsDuplicateKeyException(ex))
        {
            _logger.LogInformation(
                "WishlistProductLowStockEvent duplicate detected during inbox save. ProductId={ProductId}, MessageId={MessageId}",
                message.ProductId,
                messageId);
        }
    }

    private static void AddActivityTags(WishlistProductLowStockEvent message)
    {
        var activity = Activity.Current;
        if (activity is null)
        {
            return;
        }

        activity.SetTag("ecommerce.messaging.consumer", ConsumerName);
        activity.SetTag("ecommerce.message.type", nameof(WishlistProductLowStockEvent));
        activity.SetTag("ecommerce.product.id", message.ProductId);
        activity.SetTag("ecommerce.inventory.stock_quantity", message.StockQuantity);
        activity.SetTag("ecommerce.inventory.threshold", message.Threshold);
    }

    private static bool IsDuplicateKeyException(DbUpdateException ex)
    {
        return ex.InnerException?.Message.Contains("duplicate key", StringComparison.OrdinalIgnoreCase) == true;
    }

    private static string BuildLowStockEmailBody(
        string fullName,
        string productName,
        WishlistProductLowStockEvent message)
    {
        var greeting = string.IsNullOrWhiteSpace(fullName) ? "Merhaba" : $"Merhaba {fullName}";

        return $"""
                <p>{greeting},</p>
                <p>Favorilerinizde bulunan <strong>{productName}</strong> ürünü için stok azalıyor.</p>
                <ul>
                  <li>Kalan stok: {message.StockQuantity}</li>
                  <li>Bildirim eşiği: {message.Threshold}</li>
                </ul>
                <p>Ürünü kaçırmamak için uygulamayı ziyaret edebilirsiniz.</p>
                """;
    }
}
