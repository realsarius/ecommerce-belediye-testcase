using System.Diagnostics;
using EcommerceAPI.API.Hubs;
using EcommerceAPI.Business.Abstract;
using EcommerceAPI.DataAccess.Concrete.EntityFramework.Contexts;
using EcommerceAPI.Entities.Concrete;
using EcommerceAPI.Entities.DTOs;
using EcommerceAPI.Entities.Enums;
using EcommerceAPI.Entities.IntegrationEvents;
using MassTransit;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace EcommerceAPI.API.Consumers;

public sealed class CampaignStatusChangedConsumer : IConsumer<CampaignStatusChangedEvent>
{
    private const string ConsumerName = nameof(CampaignStatusChangedConsumer);
    private readonly AppDbContext _dbContext;
    private readonly IHubContext<WishlistHub> _hubContext;
    private readonly INotificationService _notificationService;
    private readonly ILogger<CampaignStatusChangedConsumer> _logger;

    public CampaignStatusChangedConsumer(
        AppDbContext dbContext,
        IHubContext<WishlistHub> hubContext,
        INotificationService notificationService,
        ILogger<CampaignStatusChangedConsumer> logger)
    {
        _dbContext = dbContext;
        _hubContext = hubContext;
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<CampaignStatusChangedEvent> context)
    {
        var message = context.Message;
        var messageId = context.MessageId ?? message.EventId;
        AddActivityTags(message);

        var alreadyProcessed = await _dbContext.InboxMessages.AnyAsync(
            x => x.ConsumerName == ConsumerName && x.MessageId == messageId,
            context.CancellationToken);

        if (alreadyProcessed)
        {
            return;
        }

        if (message.CurrentStatus == CampaignStatus.Ended)
        {
            var userIds = await _dbContext.WishlistItems
                .AsNoTracking()
                .Where(x => x.Product.CampaignProducts.Any(cp => cp.CampaignId == message.CampaignId))
                .Select(x => x.Wishlist.UserId)
                .Distinct()
                .ToListAsync(context.CancellationToken);

            foreach (var userId in userIds)
            {
                await _notificationService.CreateNotificationAsync(new CreateNotificationRequest
                {
                    UserId = userId,
                    Type = "Campaign",
                    Title = $"{message.CampaignName} kampanyası sona erdi",
                    Body = "Takip ettiğiniz kampanya sona erdi. Yeni fırsatları kaçırmamak için kampanya alanını kontrol edin.",
                    DeepLink = "/"
                });

                await _hubContext.Clients.Group(WishlistHub.UserGroup(userId))
                    .SendAsync(
                        "CampaignStatusChanged",
                        new
                        {
                            message.CampaignId,
                            message.CampaignName,
                            previousStatus = message.PreviousStatus.ToString(),
                            currentStatus = message.CurrentStatus.ToString(),
                            message.EndsAt,
                            message.BadgeText
                        },
                        context.CancellationToken);
            }
        }

        _logger.LogInformation(
            "Campaign analytics event. AnalyticsStream={AnalyticsStream}, AnalyticsEvent={AnalyticsEvent}, CampaignId={CampaignId}, CampaignName={CampaignName}, PreviousStatus={PreviousStatus}, CurrentStatus={CurrentStatus}, ProductCount={ProductCount}, MessageId={MessageId}, OccurredAt={OccurredAt}",
            "Campaign",
            "CampaignStatusChanged",
            message.CampaignId,
            message.CampaignName,
            message.PreviousStatus,
            message.CurrentStatus,
            message.ProductCount,
            messageId,
            message.OccurredAt);

        _dbContext.InboxMessages.Add(new InboxMessage
        {
            MessageId = messageId,
            ConsumerName = ConsumerName,
            MessageType = typeof(CampaignStatusChangedEvent).FullName ?? nameof(CampaignStatusChangedEvent),
            ProcessedOnUtc = DateTime.UtcNow
        });

        try
        {
            await _dbContext.SaveChangesAsync(context.CancellationToken);
        }
        catch (DbUpdateException ex) when (IsDuplicateKeyException(ex))
        {
            _logger.LogInformation(
                "CampaignStatusChangedEvent duplicate detected during inbox save. CampaignId={CampaignId}, MessageId={MessageId}",
                message.CampaignId,
                messageId);
        }
    }

    private static void AddActivityTags(CampaignStatusChangedEvent message)
    {
        var activity = Activity.Current;
        if (activity is null)
        {
            return;
        }

        activity.SetTag("ecommerce.messaging.consumer", ConsumerName);
        activity.SetTag("ecommerce.message.type", nameof(CampaignStatusChangedEvent));
        activity.SetTag("ecommerce.campaign.id", message.CampaignId);
    }

    private static bool IsDuplicateKeyException(DbUpdateException ex)
    {
        return ex.InnerException?.Message.Contains("duplicate key", StringComparison.OrdinalIgnoreCase) == true;
    }
}
