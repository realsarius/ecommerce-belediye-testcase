using EcommerceAPI.API.Consumers;
using EcommerceAPI.Application.Abstractions.ServiceContracts;
using EcommerceAPI.Core.Interfaces;
using EcommerceAPI.DataAccess.Concrete.EntityFramework.Contexts;
using EcommerceAPI.Entities.Concrete;
using EcommerceAPI.Entities.DTOs;
using EcommerceAPI.Entities.Enums;
using EcommerceAPI.Entities.IntegrationEvents;
using FluentAssertions;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace EcommerceAPI.UnitTests;

public class FulfillmentConsumerTests
{
    [Fact]
    public async Task OrderShippedConsumer_ShouldWriteStructuredAnalyticsLogAndInbox()
    {
        await using var dbContext = CreateDbContext();
        var emailService = new Mock<IEmailNotificationService>();
        var notificationService = new Mock<INotificationService>();
        var notificationPreferenceService = new Mock<INotificationPreferenceService>();
        var loggerMock = new Mock<ILogger<OrderShippedConsumer>>();

        notificationService
            .Setup(x => x.CreateNotificationAsync(It.IsAny<CreateNotificationRequest>()))
            .ReturnsAsync(new EcommerceAPI.Core.Utilities.Results.SuccessDataResult<NotificationDto>(new NotificationDto()));
        notificationPreferenceService
            .Setup(x => x.GetChannelSettingsAsync(42, NotificationType.Order))
            .ReturnsAsync(new NotificationChannelSettingsDto
            {
                InAppEnabled = true,
                EmailEnabled = false,
                PushEnabled = false,
                SupportsInApp = true,
                SupportsEmail = true,
                SupportsPush = true
            });

        var consumer = new OrderShippedConsumer(
            dbContext,
            emailService.Object,
            notificationService.Object,
            notificationPreferenceService.Object,
            loggerMock.Object);

        var message = new OrderShippedEvent
        {
            EventId = Guid.NewGuid(),
            OrderId = 501,
            OrderNumber = "ORD-501",
            UserId = 42,
            CargoCompany = "Yurtiçi Kargo",
            TrackingCode = "YT123456789",
            ShippedAt = DateTime.UtcNow,
            EstimatedDeliveryDate = DateTime.UtcNow.Date.AddDays(2),
            CorrelationId = "corr-ship-1"
        };

        var context = CreateConsumeContext(message);

        await consumer.Consume(context.Object);

        dbContext.InboxMessages.Should().ContainSingle(x =>
            x.ConsumerName == "OrderShippedConsumer" &&
            x.MessageId == message.EventId);
        loggerMock.VerifyStructuredLogContains(LogLevel.Information, "Shipment analytics event.");
        loggerMock.VerifyStructuredLogContains(LogLevel.Information, "order_shipped");
        loggerMock.VerifyStructuredLogContains(LogLevel.Information, "corr-ship-1");
    }

    [Fact]
    public async Task ReturnRequestReviewedConsumer_ShouldWriteStructuredAnalyticsLogAndInbox()
    {
        await using var dbContext = CreateDbContext();
        var emailService = new Mock<IEmailNotificationService>();
        var notificationService = new Mock<INotificationService>();
        var notificationPreferenceService = new Mock<INotificationPreferenceService>();
        var loggerMock = new Mock<ILogger<ReturnRequestReviewedConsumer>>();

        notificationService
            .Setup(x => x.CreateNotificationAsync(It.IsAny<CreateNotificationRequest>()))
            .ReturnsAsync(new EcommerceAPI.Core.Utilities.Results.SuccessDataResult<NotificationDto>(new NotificationDto()));
        notificationPreferenceService
            .Setup(x => x.GetChannelSettingsAsync(52, NotificationType.Refund))
            .ReturnsAsync(new NotificationChannelSettingsDto
            {
                InAppEnabled = true,
                EmailEnabled = false,
                PushEnabled = false,
                SupportsInApp = true,
                SupportsEmail = true,
                SupportsPush = true
            });

        var consumer = new ReturnRequestReviewedConsumer(
            dbContext,
            emailService.Object,
            notificationService.Object,
            notificationPreferenceService.Object,
            loggerMock.Object);

        var message = new ReturnRequestReviewedEvent
        {
            EventId = Guid.NewGuid(),
            ReturnRequestId = 601,
            OrderId = 301,
            OrderNumber = "ORD-301",
            UserId = 52,
            Decision = "Approved",
            CurrentStatus = "RefundPending",
            CorrelationId = "corr-return-1"
        };

        var context = CreateConsumeContext(message);

        await consumer.Consume(context.Object);

        dbContext.InboxMessages.Should().ContainSingle(x =>
            x.ConsumerName == "ReturnRequestReviewedConsumer" &&
            x.MessageId == message.EventId);
        loggerMock.VerifyStructuredLogContains(LogLevel.Information, "Return analytics event.");
        loggerMock.VerifyStructuredLogContains(LogLevel.Information, "return_request_reviewed");
        loggerMock.VerifyStructuredLogContains(LogLevel.Information, "corr-return-1");
    }

    private static AppDbContext CreateDbContext()
    {
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        Microsoft.EntityFrameworkCore.InMemoryDbContextOptionsExtensions.UseInMemoryDatabase(
            optionsBuilder,
            Guid.NewGuid().ToString("N"));
        return new AppDbContext(optionsBuilder.Options);
    }

    private static Mock<ConsumeContext<TMessage>> CreateConsumeContext<TMessage>(TMessage message)
        where TMessage : class
    {
        var context = new Mock<ConsumeContext<TMessage>>();
        context.SetupGet(x => x.Message).Returns(message);
        context.SetupGet(x => x.MessageId).Returns(message switch
        {
            OrderShippedEvent orderShipped => orderShipped.EventId,
            ReturnRequestReviewedEvent returnReviewed => returnReviewed.EventId,
            _ => Guid.NewGuid()
        });
        context.SetupGet(x => x.CancellationToken).Returns(CancellationToken.None);
        return context;
    }
}

internal static class FulfillmentConsumerLoggerExtensions
{
    public static void VerifyStructuredLogContains<T>(this Mock<ILogger<T>> loggerMock, LogLevel level, string expectedValue)
    {
        loggerMock.Verify(
            logger => logger.Log(
                level,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((state, _) => state.ToString()!.Contains(expectedValue, StringComparison.Ordinal)),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }
}
