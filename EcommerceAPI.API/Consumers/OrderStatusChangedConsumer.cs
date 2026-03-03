using System.Diagnostics;
using EcommerceAPI.Business.Abstract;
using EcommerceAPI.Core.Interfaces;
using EcommerceAPI.DataAccess.Concrete.EntityFramework.Contexts;
using EcommerceAPI.Entities.Concrete;
using EcommerceAPI.Entities.DTOs;
using EcommerceAPI.Entities.Enums;
using EcommerceAPI.Entities.IntegrationEvents;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace EcommerceAPI.API.Consumers;

public sealed class OrderStatusChangedConsumer : IConsumer<OrderStatusChangedEvent>
{
    private const string ConsumerName = nameof(OrderStatusChangedConsumer);

    private readonly AppDbContext _dbContext;
    private readonly IEmailNotificationService _emailNotificationService;
    private readonly INotificationService _notificationService;
    private readonly INotificationPreferenceService _notificationPreferenceService;
    private readonly ILogger<OrderStatusChangedConsumer> _logger;

    public OrderStatusChangedConsumer(
        AppDbContext dbContext,
        IEmailNotificationService emailNotificationService,
        INotificationService notificationService,
        INotificationPreferenceService notificationPreferenceService,
        ILogger<OrderStatusChangedConsumer> logger)
    {
        _dbContext = dbContext;
        _emailNotificationService = emailNotificationService;
        _notificationService = notificationService;
        _notificationPreferenceService = notificationPreferenceService;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<OrderStatusChangedEvent> context)
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
                "OrderStatusChangedEvent duplicate skipped. OrderId={OrderId}, MessageId={MessageId}",
                message.OrderId,
                messageId);
            return;
        }

        var channelSettings = await _notificationPreferenceService.GetChannelSettingsAsync(
            message.UserId,
            NotificationType.Order);

        var previousLabel = GetStatusLabel(message.PreviousStatus);
        var newLabel = GetStatusLabel(message.NewStatus);
        var notificationBody = $"{message.OrderNumber} numaralı siparişinizin durumu {previousLabel} aşamasından {newLabel} aşamasına güncellendi.";

        if (channelSettings.InAppEnabled)
        {
            await _notificationService.CreateNotificationAsync(new CreateNotificationRequest
            {
                UserId = message.UserId,
                Type = "Order",
                Title = "Sipariş durumunuz güncellendi",
                Body = notificationBody,
                DeepLink = $"/orders/{message.OrderId}"
            });
        }

        if (channelSettings.EmailEnabled && !string.IsNullOrWhiteSpace(message.CustomerEmail))
        {
            await _emailNotificationService.SendAsync(
                message.CustomerEmail,
                $"{message.OrderNumber} sipariş durumunuz güncellendi",
                BuildStatusChangeEmailBody(message, previousLabel, newLabel),
                context.CancellationToken);
        }

        _dbContext.InboxMessages.Add(new InboxMessage
        {
            MessageId = messageId,
            ConsumerName = ConsumerName,
            MessageType = typeof(OrderStatusChangedEvent).FullName ?? nameof(OrderStatusChangedEvent),
            ProcessedOnUtc = DateTime.UtcNow
        });

        try
        {
            await _dbContext.SaveChangesAsync(context.CancellationToken);
        }
        catch (DbUpdateException ex) when (IsDuplicateKeyException(ex))
        {
            _logger.LogInformation(
                "OrderStatusChangedEvent duplicate detected during inbox save. OrderId={OrderId}, MessageId={MessageId}",
                message.OrderId,
                messageId);
        }
    }

    private static void AddActivityTags(OrderStatusChangedEvent message)
    {
        var activity = Activity.Current;
        if (activity is null)
        {
            return;
        }

        activity.SetTag("ecommerce.messaging.consumer", ConsumerName);
        activity.SetTag("ecommerce.message.type", nameof(OrderStatusChangedEvent));
        activity.SetTag("ecommerce.order.id", message.OrderId);
        activity.SetTag("ecommerce.order.number", message.OrderNumber);
        activity.SetTag("ecommerce.order.status.previous", message.PreviousStatus);
        activity.SetTag("ecommerce.order.status.new", message.NewStatus);
    }

    private static string GetStatusLabel(string status)
    {
        return status switch
        {
            "PendingPayment" => "Ödeme Bekleniyor",
            "Paid" => "Ödendi",
            "Processing" => "Hazırlanıyor",
            "Shipped" => "Kargoda",
            "Delivered" => "Teslim Edildi",
            "Cancelled" => "İptal Edildi",
            "Refunded" => "İade Edildi",
            _ => status
        };
    }

    private static string BuildStatusChangeEmailBody(
        OrderStatusChangedEvent message,
        string previousLabel,
        string newLabel)
    {
        var greeting = string.IsNullOrWhiteSpace(message.CustomerName) ? "Merhaba" : $"Merhaba {message.CustomerName}";

        return $"""
                <p>{greeting},</p>
                <p><strong>{message.OrderNumber}</strong> numaralı siparişinizin durumu güncellendi.</p>
                <ul>
                  <li>Önceki durum: {previousLabel}</li>
                  <li>Yeni durum: {newLabel}</li>
                  <li>Güncellenme zamanı: {message.ChangedAt.ToLocalTime():dd.MM.yyyy HH:mm}</li>
                </ul>
                <p>Sipariş detaylarınızı hesabınızdan takip edebilirsiniz.</p>
                """;
    }

    private static bool IsDuplicateKeyException(DbUpdateException ex)
    {
        return ex.InnerException?.Message.Contains("duplicate key", StringComparison.OrdinalIgnoreCase) == true;
    }
}
