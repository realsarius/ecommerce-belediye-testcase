using System.Diagnostics;
using EcommerceAPI.Application.Abstractions.ServiceContracts;
using EcommerceAPI.DataAccess.Concrete.EntityFramework.Contexts;
using EcommerceAPI.Entities.Concrete;
using EcommerceAPI.Entities.IntegrationEvents;
using Hangfire;
using Hangfire.Common;
using Hangfire.States;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace EcommerceAPI.API.Consumers;

public sealed class AnnouncementCreatedConsumer : IConsumer<AnnouncementCreatedEvent>
{
    private const string ConsumerName = nameof(AnnouncementCreatedConsumer);

    private readonly AppDbContext _dbContext;
    private readonly IAnnouncementService _announcementService;
    private readonly IBackgroundJobClient _backgroundJobClient;
    private readonly ILogger<AnnouncementCreatedConsumer> _logger;

    public AnnouncementCreatedConsumer(
        AppDbContext dbContext,
        IAnnouncementService announcementService,
        IBackgroundJobClient backgroundJobClient,
        ILogger<AnnouncementCreatedConsumer> logger)
    {
        _dbContext = dbContext;
        _announcementService = announcementService;
        _backgroundJobClient = backgroundJobClient;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<AnnouncementCreatedEvent> context)
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
                "AnnouncementCreatedEvent duplicate skipped. AnnouncementId={AnnouncementId}, MessageId={MessageId}",
                message.AnnouncementId,
                messageId);
            return;
        }

        var announcementResult = await _announcementService.GetByIdAsync(message.AnnouncementId);
        if (!announcementResult.Success || announcementResult.Data == null)
        {
            throw new InvalidOperationException($"Announcement {message.AnnouncementId} could not be loaded for dispatch.");
        }

        var announcement = announcementResult.Data;
        if (announcement.SentAt.HasValue && announcement.Status is "Sent" or "PartiallySent")
        {
            await SaveInboxAsync(messageId, context.CancellationToken);
            return;
        }

        var scheduledAt = announcement.ScheduledAt;
        if (scheduledAt.HasValue && scheduledAt.Value > DateTime.UtcNow.AddMinutes(1))
        {
            _backgroundJobClient.Create(
                Job.FromExpression<IAnnouncementService>(service => service.SendAnnouncementAsync(message.AnnouncementId)),
                new ScheduledState(scheduledAt.Value));

            _logger.LogInformation(
                "Announcement delivery scheduled via Hangfire. AnnouncementId={AnnouncementId}, ScheduledAt={ScheduledAt}",
                message.AnnouncementId,
                scheduledAt.Value);
        }
        else
        {
            await _announcementService.SendAnnouncementAsync(message.AnnouncementId);
        }

        await SaveInboxAsync(messageId, context.CancellationToken);
    }

    private async Task SaveInboxAsync(Guid messageId, CancellationToken cancellationToken)
    {
        _dbContext.InboxMessages.Add(new InboxMessage
        {
            MessageId = messageId,
            ConsumerName = ConsumerName,
            MessageType = typeof(AnnouncementCreatedEvent).FullName ?? nameof(AnnouncementCreatedEvent),
            ProcessedOnUtc = DateTime.UtcNow
        });

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (IsDuplicateKeyException(ex))
        {
            _logger.LogInformation(
                "AnnouncementCreatedEvent duplicate detected during inbox save. MessageId={MessageId}",
                messageId);
        }
    }

    private static void AddActivityTags(AnnouncementCreatedEvent message)
    {
        var activity = Activity.Current;
        if (activity is null)
        {
            return;
        }

        activity.SetTag("ecommerce.messaging.consumer", ConsumerName);
        activity.SetTag("ecommerce.message.type", nameof(AnnouncementCreatedEvent));
        activity.SetTag("ecommerce.announcement.id", message.AnnouncementId);
        activity.SetTag("ecommerce.announcement.scheduled_at", message.ScheduledAt);
    }

    private static bool IsDuplicateKeyException(DbUpdateException ex)
    {
        return ex.InnerException?.Message.Contains("duplicate key", StringComparison.OrdinalIgnoreCase) == true;
    }
}
