using System.Diagnostics;
using EcommerceAPI.Business.Abstract;
using EcommerceAPI.Core.Interfaces;
using EcommerceAPI.DataAccess.Concrete.EntityFramework.Contexts;
using EcommerceAPI.Entities.Concrete;
using EcommerceAPI.Entities.DTOs;
using EcommerceAPI.Entities.Enums;
using EcommerceAPI.Entities.IntegrationEvents;
using EcommerceAPI.Entities.Utilities;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace EcommerceAPI.API.Consumers;

public sealed class ReturnRequestReviewedConsumer : IConsumer<ReturnRequestReviewedEvent>
{
    private const string ConsumerName = nameof(ReturnRequestReviewedConsumer);

    private readonly AppDbContext _dbContext;
    private readonly IEmailNotificationService _emailNotificationService;
    private readonly INotificationService _notificationService;
    private readonly INotificationPreferenceService _notificationPreferenceService;
    private readonly ILogger<ReturnRequestReviewedConsumer> _logger;

    public ReturnRequestReviewedConsumer(
        AppDbContext dbContext,
        IEmailNotificationService emailNotificationService,
        INotificationService notificationService,
        INotificationPreferenceService notificationPreferenceService,
        ILogger<ReturnRequestReviewedConsumer> logger)
    {
        _dbContext = dbContext;
        _emailNotificationService = emailNotificationService;
        _notificationService = notificationService;
        _notificationPreferenceService = notificationPreferenceService;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<ReturnRequestReviewedEvent> context)
    {
        var message = context.Message;
        var messageId = context.MessageId ?? message.EventId;

        AddActivityTags(message);

        var alreadyProcessed = await _dbContext.InboxMessages.AnyAsync(
            item => item.ConsumerName == ConsumerName && item.MessageId == messageId,
            context.CancellationToken);

        if (alreadyProcessed)
        {
            _logger.LogInformation(
                "ReturnRequestReviewedEvent duplicate skipped. ReturnRequestId={ReturnRequestId}, MessageId={MessageId}, CorrelationId={CorrelationId}",
                message.ReturnRequestId,
                messageId,
                message.CorrelationId);
            return;
        }

        var channelSettings = await _notificationPreferenceService.GetChannelSettingsAsync(
            message.UserId,
            NotificationType.Refund);

        var title = message.Decision == "Approved"
            ? "İade talebiniz onaylandı"
            : "İade talebiniz reddedildi";
        var body = BuildNotificationBody(message);

        if (channelSettings.InAppEnabled)
        {
            await _notificationService.CreateNotificationAsync(new CreateNotificationRequest
            {
                UserId = message.UserId,
                Type = "Refund",
                Title = title,
                Body = body,
                DeepLink = $"/returns"
            });
        }

        if (channelSettings.EmailEnabled && !string.IsNullOrWhiteSpace(message.UserEmail))
        {
            await _emailNotificationService.SendAsync(
                message.UserEmail,
                title,
                BuildEmailBody(message, body),
                context.CancellationToken);
        }

        _logger.LogInformation(
            "Return analytics event. AnalyticsStream={AnalyticsStream}, AnalyticsEvent={AnalyticsEvent}, ReturnRequestId={ReturnRequestId}, OrderId={OrderId}, UserId={UserId}, Decision={Decision}, Status={Status}, MessageId={MessageId}, OccurredAt={OccurredAt}, CorrelationId={CorrelationId}",
            AnalyticsLogSchema.Streams.Returns,
            AnalyticsLogSchema.Events.ReturnRequestReviewed,
            message.ReturnRequestId,
            message.OrderId,
            message.UserId,
            message.Decision,
            message.CurrentStatus,
            messageId,
            DateTime.UtcNow,
            message.CorrelationId);

        _dbContext.InboxMessages.Add(new InboxMessage
        {
            MessageId = messageId,
            ConsumerName = ConsumerName,
            MessageType = typeof(ReturnRequestReviewedEvent).FullName ?? nameof(ReturnRequestReviewedEvent),
            ProcessedOnUtc = DateTime.UtcNow
        });

        try
        {
            await _dbContext.SaveChangesAsync(context.CancellationToken);
        }
        catch (DbUpdateException ex) when (IsDuplicateKeyException(ex))
        {
            _logger.LogInformation(
                "ReturnRequestReviewedEvent duplicate detected during inbox save. ReturnRequestId={ReturnRequestId}, MessageId={MessageId}, CorrelationId={CorrelationId}",
                message.ReturnRequestId,
                messageId,
                message.CorrelationId);
        }
    }

    private static string BuildNotificationBody(ReturnRequestReviewedEvent message)
    {
        var statusLabel = message.CurrentStatus switch
        {
            "RefundPending" => "Refund işlemi sıraya alındı.",
            "Refunded" => "İade tutarı ödeme sistemine aktarıldı.",
            "Approved" => "Talebiniz onaylandı.",
            "Rejected" => "Talebiniz reddedildi.",
            _ => $"Talep durumu {message.CurrentStatus} olarak güncellendi."
        };

        return string.IsNullOrWhiteSpace(message.ReviewNote)
            ? $"{message.OrderNumber} numaralı siparişiniz için iade talebiniz değerlendirildi. {statusLabel}"
            : $"{message.OrderNumber} numaralı siparişiniz için iade talebiniz değerlendirildi. {statusLabel} Not: {message.ReviewNote}";
    }

    private static string BuildEmailBody(ReturnRequestReviewedEvent message, string body)
    {
        var greeting = string.IsNullOrWhiteSpace(message.CustomerName) ? "Merhaba" : $"Merhaba {message.CustomerName}";

        return $"""
                <p>{greeting},</p>
                <p>{body}</p>
                <p>Detayları hesabınızdaki iade talepleri ekranından takip edebilirsiniz.</p>
                """;
    }

    private static void AddActivityTags(ReturnRequestReviewedEvent message)
    {
        var activity = Activity.Current;
        if (activity is null)
        {
            return;
        }

        activity.SetTag("ecommerce.messaging.consumer", ConsumerName);
        activity.SetTag("ecommerce.message.type", nameof(ReturnRequestReviewedEvent));
        activity.SetTag("ecommerce.return_request.id", message.ReturnRequestId);
        activity.SetTag("ecommerce.return_request.status", message.CurrentStatus);
        activity.SetTag("ecommerce.correlation_id", message.CorrelationId);
    }

    private static bool IsDuplicateKeyException(DbUpdateException ex)
    {
        return ex.InnerException?.Message.Contains("duplicate key", StringComparison.OrdinalIgnoreCase) == true;
    }
}
