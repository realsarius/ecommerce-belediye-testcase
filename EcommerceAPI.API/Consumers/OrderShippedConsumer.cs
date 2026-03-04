using System.Diagnostics;
using EcommerceAPI.Business.Abstract;
using EcommerceAPI.Core.Interfaces;
using EcommerceAPI.DataAccess.Concrete.EntityFramework.Contexts;
using EcommerceAPI.Entities.Concrete;
using EcommerceAPI.Entities.DTOs;
using EcommerceAPI.Entities.IntegrationEvents;
using EcommerceAPI.Entities.Utilities;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace EcommerceAPI.API.Consumers;

public sealed class OrderShippedConsumer : IConsumer<OrderShippedEvent>
{
    private const string ConsumerName = nameof(OrderShippedConsumer);

    private readonly AppDbContext _dbContext;
    private readonly IEmailNotificationService _emailNotificationService;
    private readonly INotificationService _notificationService;
    private readonly INotificationPreferenceService _notificationPreferenceService;
    private readonly ILogger<OrderShippedConsumer> _logger;

    public OrderShippedConsumer(
        AppDbContext dbContext,
        IEmailNotificationService emailNotificationService,
        INotificationService notificationService,
        INotificationPreferenceService notificationPreferenceService,
        ILogger<OrderShippedConsumer> logger)
    {
        _dbContext = dbContext;
        _emailNotificationService = emailNotificationService;
        _notificationService = notificationService;
        _notificationPreferenceService = notificationPreferenceService;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<OrderShippedEvent> context)
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
                "OrderShippedEvent duplicate skipped. OrderId={OrderId}, MessageId={MessageId}, CorrelationId={CorrelationId}",
                message.OrderId,
                messageId,
                message.CorrelationId);
            return;
        }

        var channelSettings = await _notificationPreferenceService.GetChannelSettingsAsync(
            message.UserId,
            Entities.Enums.NotificationType.Order);

        if (channelSettings.InAppEnabled)
        {
            var estimatedDeliveryText = message.EstimatedDeliveryDate.HasValue
                ? $" Tahmini teslimat: {message.EstimatedDeliveryDate.Value.ToLocalTime():dd.MM.yyyy}."
                : string.Empty;

            await _notificationService.CreateNotificationAsync(new CreateNotificationRequest
            {
                UserId = message.UserId,
                Type = "Order",
                Title = "Siparişiniz kargoya verildi",
                Body = $"{message.OrderNumber} numaralı siparişiniz {message.CargoCompany} ile yola çıktı. Takip kodu: {message.TrackingCode}.{estimatedDeliveryText}",
                DeepLink = $"/orders/{message.OrderId}"
            });
        }

        if (channelSettings.EmailEnabled && !string.IsNullOrWhiteSpace(message.CustomerEmail))
        {
            await _emailNotificationService.SendAsync(
                message.CustomerEmail,
                $"{message.OrderNumber} siparişiniz kargoya verildi",
                BuildShipmentEmailBody(message),
                context.CancellationToken);
        }

        _logger.LogInformation(
            "Shipment analytics event. AnalyticsStream={AnalyticsStream}, AnalyticsEvent={AnalyticsEvent}, OrderId={OrderId}, OrderNumber={OrderNumber}, UserId={UserId}, CargoCompany={CargoCompany}, TrackingCode={TrackingCode}, EstimatedDeliveryDate={EstimatedDeliveryDate}, MessageId={MessageId}, OccurredAt={OccurredAt}, CorrelationId={CorrelationId}",
            AnalyticsLogSchema.Streams.Fulfillment,
            AnalyticsLogSchema.Events.OrderShipped,
            message.OrderId,
            message.OrderNumber,
            message.UserId,
            message.CargoCompany,
            message.TrackingCode,
            message.EstimatedDeliveryDate,
            messageId,
            message.ShippedAt,
            message.CorrelationId);

        _dbContext.InboxMessages.Add(new InboxMessage
        {
            MessageId = messageId,
            ConsumerName = ConsumerName,
            MessageType = typeof(OrderShippedEvent).FullName ?? nameof(OrderShippedEvent),
            ProcessedOnUtc = DateTime.UtcNow
        });

        try
        {
            await _dbContext.SaveChangesAsync(context.CancellationToken);
        }
        catch (DbUpdateException ex) when (IsDuplicateKeyException(ex))
        {
            _logger.LogInformation(
                "OrderShippedEvent duplicate detected during inbox save. OrderId={OrderId}, MessageId={MessageId}, CorrelationId={CorrelationId}",
                message.OrderId,
                messageId,
                message.CorrelationId);
        }
    }

    private static void AddActivityTags(OrderShippedEvent message)
    {
        var activity = Activity.Current;
        if (activity is null)
        {
            return;
        }

        activity.SetTag("ecommerce.messaging.consumer", ConsumerName);
        activity.SetTag("ecommerce.message.type", nameof(OrderShippedEvent));
        activity.SetTag("ecommerce.order.id", message.OrderId);
        activity.SetTag("ecommerce.order.number", message.OrderNumber);
        activity.SetTag("ecommerce.correlation_id", message.CorrelationId);
    }

    private static bool IsDuplicateKeyException(DbUpdateException ex)
    {
        return ex.InnerException?.Message.Contains("duplicate key", StringComparison.OrdinalIgnoreCase) == true;
    }

    private static string BuildShipmentEmailBody(OrderShippedEvent message)
    {
        var greeting = string.IsNullOrWhiteSpace(message.CustomerName) ? "Merhaba" : $"Merhaba {message.CustomerName}";

        return $"""
                <p>{greeting},</p>
                <p><strong>{message.OrderNumber}</strong> numaralı siparişiniz kargoya verildi.</p>
                <ul>
                  <li>Kargo firması: {message.CargoCompany}</li>
                  <li>Takip kodu: {message.TrackingCode}</li>
                  <li>Gönderim zamanı: {message.ShippedAt.ToLocalTime():dd.MM.yyyy HH:mm}</li>
                  {(message.EstimatedDeliveryDate.HasValue ? $"<li>Tahmini teslimat: {message.EstimatedDeliveryDate.Value.ToLocalTime():dd.MM.yyyy}</li>" : string.Empty)}
                </ul>
                <p>Sipariş detaylarınızı hesabınızdan takip edebilirsiniz.</p>
                """;
    }
}
