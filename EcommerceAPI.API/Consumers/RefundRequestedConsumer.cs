using System.Diagnostics;
using EcommerceAPI.Business.Abstract;
using EcommerceAPI.Core.Interfaces;
using EcommerceAPI.DataAccess.Concrete.EntityFramework.Contexts;
using EcommerceAPI.Entities.Concrete;
using EcommerceAPI.Entities.IntegrationEvents;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace EcommerceAPI.API.Consumers;

public sealed class RefundRequestedConsumer : IConsumer<RefundRequestedEvent>
{
    private const string ConsumerName = nameof(RefundRequestedConsumer);
    private readonly AppDbContext _dbContext;
    private readonly IRefundService _refundService;
    private readonly IEmailNotificationService _emailNotificationService;
    private readonly ILogger<RefundRequestedConsumer> _logger;

    public RefundRequestedConsumer(
        AppDbContext dbContext,
        IRefundService refundService,
        IEmailNotificationService emailNotificationService,
        ILogger<RefundRequestedConsumer> logger)
    {
        _dbContext = dbContext;
        _refundService = refundService;
        _emailNotificationService = emailNotificationService;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<RefundRequestedEvent> context)
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
                "RefundRequestedEvent duplicate skipped. RefundRequestId={RefundRequestId}, MessageId={MessageId}",
                message.RefundRequestId,
                messageId);
            return;
        }

        var result = await _refundService.ProcessRefundAsync(message.RefundRequestId, context.CancellationToken);

        if (result.Success)
        {
            await _emailNotificationService.SendAsync(
                result.Data.CustomerEmail,
                $"{result.Data.OrderNumber} siparişiniz için iade tamamlandı",
                BuildRefundSucceededEmailBody(result.Data),
                context.CancellationToken);

            _logger.LogInformation(
                "Refund analytics event. AnalyticsStream={AnalyticsStream}, AnalyticsEvent={AnalyticsEvent}, RefundRequestId={RefundRequestId}, ReturnRequestId={ReturnRequestId}, OrderId={OrderId}, UserId={UserId}, Amount={Amount}, Currency={Currency}, Status={Status}, MessageId={MessageId}, ProcessedAt={ProcessedAt}",
                "Refund",
                "RefundProcessed",
                result.Data.Id,
                result.Data.ReturnRequestId,
                result.Data.OrderId,
                result.Data.UserId,
                result.Data.Amount,
                result.Data.Currency,
                result.Data.Status,
                messageId,
                result.Data.ProcessedAt);
        }
        else
        {
            if (result.Data != null && !string.IsNullOrWhiteSpace(result.Data.CustomerEmail))
            {
                await _emailNotificationService.SendAsync(
                    result.Data.CustomerEmail,
                    $"{result.Data.OrderNumber} siparişiniz için iade işlenemedi",
                    BuildRefundFailedEmailBody(result.Data),
                    context.CancellationToken);
            }

            _logger.LogWarning(
                "Refund analytics event. AnalyticsStream={AnalyticsStream}, AnalyticsEvent={AnalyticsEvent}, RefundRequestId={RefundRequestId}, ReturnRequestId={ReturnRequestId}, OrderId={OrderId}, UserId={UserId}, Amount={Amount}, Currency={Currency}, Status={Status}, FailureReason={FailureReason}, MessageId={MessageId}",
                "Refund",
                "RefundFailed",
                result.Data?.Id ?? message.RefundRequestId,
                result.Data?.ReturnRequestId ?? message.ReturnRequestId,
                result.Data?.OrderId ?? message.OrderId,
                result.Data?.UserId ?? message.UserId,
                result.Data?.Amount ?? message.Amount,
                result.Data?.Currency ?? message.Currency,
                result.Data?.Status ?? "Failed",
                result.Data?.FailureReason ?? result.Message,
                messageId);
        }

        _dbContext.InboxMessages.Add(new InboxMessage
        {
            MessageId = messageId,
            ConsumerName = ConsumerName,
            MessageType = typeof(RefundRequestedEvent).FullName ?? nameof(RefundRequestedEvent),
            ProcessedOnUtc = DateTime.UtcNow
        });

        try
        {
            await _dbContext.SaveChangesAsync(context.CancellationToken);
        }
        catch (DbUpdateException ex) when (IsDuplicateKeyException(ex))
        {
            _logger.LogInformation(
                "RefundRequestedEvent duplicate detected during inbox save. RefundRequestId={RefundRequestId}, MessageId={MessageId}",
                message.RefundRequestId,
                messageId);
        }
    }

    private static void AddActivityTags(RefundRequestedEvent message)
    {
        var activity = Activity.Current;
        if (activity is null)
        {
            return;
        }

        activity.SetTag("ecommerce.messaging.consumer", ConsumerName);
        activity.SetTag("ecommerce.message.type", nameof(RefundRequestedEvent));
        activity.SetTag("ecommerce.refund_request.id", message.RefundRequestId);
        activity.SetTag("ecommerce.order.id", message.OrderId);
    }

    private static bool IsDuplicateKeyException(DbUpdateException ex)
    {
        return ex.InnerException?.Message.Contains("duplicate key", StringComparison.OrdinalIgnoreCase) == true;
    }

    private static string BuildRefundSucceededEmailBody(Entities.DTOs.RefundRequestDto refund)
    {
        var greeting = string.IsNullOrWhiteSpace(refund.CustomerName) ? "Merhaba" : $"Merhaba {refund.CustomerName}";

        return $"""
                <p>{greeting},</p>
                <p><strong>{refund.OrderNumber}</strong> numaralı siparişiniz için iade işlemi tamamlandı.</p>
                <ul>
                  <li>İade tutarı: {refund.Amount:N2} {refund.Currency}</li>
                  <li>Durum: {refund.Status}</li>
                </ul>
                <p>İadenin hesabınıza yansıması bankanıza bağlı olarak değişebilir.</p>
                """;
    }

    private static string BuildRefundFailedEmailBody(Entities.DTOs.RefundRequestDto refund)
    {
        var greeting = string.IsNullOrWhiteSpace(refund.CustomerName) ? "Merhaba" : $"Merhaba {refund.CustomerName}";

        return $"""
                <p>{greeting},</p>
                <p><strong>{refund.OrderNumber}</strong> numaralı siparişiniz için iade işlemi tamamlanamadı.</p>
                <ul>
                  <li>İade tutarı: {refund.Amount:N2} {refund.Currency}</li>
                  <li>Neden: {refund.FailureReason ?? "Bilinmeyen hata"}</li>
                </ul>
                <p>Destek ekibimiz gerekli incelemeyi yapacaktır.</p>
                """;
    }
}
