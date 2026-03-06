using System.Diagnostics;
using EcommerceAPI.Application.Abstractions.ServiceContracts;
using EcommerceAPI.Core.Interfaces;
using EcommerceAPI.DataAccess.Concrete.EntityFramework.Contexts;
using EcommerceAPI.Entities.Concrete;
using EcommerceAPI.Entities.DTOs;
using EcommerceAPI.Entities.Enums;
using EcommerceAPI.Entities.IntegrationEvents;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace EcommerceAPI.API.Consumers;

public sealed class SellerApplicationReviewedConsumer : IConsumer<SellerApplicationReviewedEvent>
{
    private const string ConsumerName = nameof(SellerApplicationReviewedConsumer);

    private readonly AppDbContext _dbContext;
    private readonly IEmailNotificationService _emailNotificationService;
    private readonly INotificationService _notificationService;
    private readonly INotificationPreferenceService _notificationPreferenceService;
    private readonly ILogger<SellerApplicationReviewedConsumer> _logger;

    public SellerApplicationReviewedConsumer(
        AppDbContext dbContext,
        IEmailNotificationService emailNotificationService,
        INotificationService notificationService,
        INotificationPreferenceService notificationPreferenceService,
        ILogger<SellerApplicationReviewedConsumer> logger)
    {
        _dbContext = dbContext;
        _emailNotificationService = emailNotificationService;
        _notificationService = notificationService;
        _notificationPreferenceService = notificationPreferenceService;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<SellerApplicationReviewedEvent> context)
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
                "SellerApplicationReviewedEvent duplicate skipped. SellerProfileId={SellerProfileId}, MessageId={MessageId}",
                message.SellerProfileId,
                messageId);
            return;
        }

        var channelSettings = await _notificationPreferenceService.GetChannelSettingsAsync(
            message.UserId,
            NotificationType.Announcement);

        var decisionLabel = message.Decision == "Approved" ? "onaylandı" : "reddedildi";
        var notificationTitle = message.Decision == "Approved"
            ? "Seller başvurunuz onaylandı"
            : "Seller başvurunuz reddedildi";
        var notificationBody = message.Decision == "Approved"
            ? $"{message.BrandName} mağazası için yaptığınız seller başvurusu onaylandı."
            : $"{message.BrandName} mağazası için yaptığınız seller başvurusu reddedildi.";

        if (channelSettings.InAppEnabled)
        {
            await _notificationService.CreateNotificationAsync(new CreateNotificationRequest
            {
                UserId = message.UserId,
                Type = "Announcement",
                Title = notificationTitle,
                Body = string.IsNullOrWhiteSpace(message.ReviewNote)
                    ? notificationBody
                    : $"{notificationBody} Not: {message.ReviewNote}",
                DeepLink = "/seller/profile"
            });
        }

        if (channelSettings.EmailEnabled && !string.IsNullOrWhiteSpace(message.UserEmail))
        {
            await _emailNotificationService.SendAsync(
                message.UserEmail,
                notificationTitle,
                BuildEmailBody(message, decisionLabel),
                context.CancellationToken);
        }

        _dbContext.InboxMessages.Add(new InboxMessage
        {
            MessageId = messageId,
            ConsumerName = ConsumerName,
            MessageType = typeof(SellerApplicationReviewedEvent).FullName ?? nameof(SellerApplicationReviewedEvent),
            ProcessedOnUtc = DateTime.UtcNow
        });

        try
        {
            await _dbContext.SaveChangesAsync(context.CancellationToken);
        }
        catch (DbUpdateException ex) when (IsDuplicateKeyException(ex))
        {
            _logger.LogInformation(
                "SellerApplicationReviewedEvent duplicate detected during inbox save. SellerProfileId={SellerProfileId}, MessageId={MessageId}",
                message.SellerProfileId,
                messageId);
        }
    }

    private static string BuildEmailBody(SellerApplicationReviewedEvent message, string decisionLabel)
    {
        var greeting = string.IsNullOrWhiteSpace(message.CustomerName) ? "Merhaba" : $"Merhaba {message.CustomerName}";
        var noteSection = string.IsNullOrWhiteSpace(message.ReviewNote)
            ? string.Empty
            : $"<p><strong>Not:</strong> {message.ReviewNote}</p>";

        return $"""
                <p>{greeting},</p>
                <p><strong>{message.BrandName}</strong> mağazası için yaptığınız seller başvurusu {decisionLabel}.</p>
                {noteSection}
                <p>Detayları seller panelinizden takip edebilirsiniz.</p>
                """;
    }

    private static void AddActivityTags(SellerApplicationReviewedEvent message)
    {
        var activity = Activity.Current;
        if (activity is null)
        {
            return;
        }

        activity.SetTag("ecommerce.messaging.consumer", ConsumerName);
        activity.SetTag("ecommerce.message.type", nameof(SellerApplicationReviewedEvent));
        activity.SetTag("ecommerce.seller.id", message.SellerProfileId);
        activity.SetTag("ecommerce.seller.decision", message.Decision);
    }

    private static bool IsDuplicateKeyException(DbUpdateException ex)
    {
        return ex.InnerException?.Message.Contains("duplicate key", StringComparison.OrdinalIgnoreCase) == true;
    }
}
