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
using EcommerceAPI.Entities.Utilities;

namespace EcommerceAPI.UnitTests;

public class RefundConsumerTests
{
    [Fact]
    public async Task RefundRequestedConsumer_WhenRefundSucceeds_ShouldSendEmailAndSaveInbox()
    {
        await using var dbContext = CreateDbContext();
        var refundService = new Mock<IRefundService>();
        var refundRetryScheduler = new Mock<IRefundRetryScheduler>();
        refundService.Setup(x => x.ProcessRefundAsync(501, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EcommerceAPI.Core.Utilities.Results.SuccessDataResult<RefundRequestDto>(new RefundRequestDto
            {
                Id = 501,
                ReturnRequestId = 301,
                OrderId = 201,
                UserId = 42,
                OrderNumber = "ORD-201",
                CustomerEmail = "customer@test.com",
                CustomerName = "Test Customer",
                Amount = 99.90m,
                Currency = "TRY",
                Status = "Succeeded",
                ProcessedAt = DateTime.UtcNow
            }));

        var emailService = new Mock<IEmailNotificationService>();
        emailService
            .Setup(x => x.SendAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        var notificationService = new Mock<INotificationService>();
        notificationService
            .Setup(x => x.CreateNotificationAsync(It.IsAny<CreateNotificationRequest>()))
            .ReturnsAsync(new EcommerceAPI.Core.Utilities.Results.SuccessDataResult<NotificationDto>(new NotificationDto()));
        var notificationPreferenceService = new Mock<INotificationPreferenceService>();
        var loggerMock = new Mock<ILogger<RefundRequestedConsumer>>();
        notificationPreferenceService
            .Setup(x => x.GetChannelSettingsAsync(42, NotificationType.Refund))
            .ReturnsAsync(new NotificationChannelSettingsDto
            {
                InAppEnabled = true,
                EmailEnabled = true,
                PushEnabled = false,
                SupportsInApp = true,
                SupportsEmail = true,
                SupportsPush = true
            });

        var consumer = new RefundRequestedConsumer(
            dbContext,
            refundService.Object,
            refundRetryScheduler.Object,
            emailService.Object,
            notificationService.Object,
            notificationPreferenceService.Object,
            loggerMock.Object);

        var message = new RefundRequestedEvent
        {
            EventId = Guid.NewGuid(),
            RefundRequestId = 501,
            ReturnRequestId = 301,
            OrderId = 201,
            UserId = 42,
            Amount = 99.90m,
            Currency = "TRY"
        };

        var context = CreateConsumeContext(message);

        await consumer.Consume(context.Object);

        emailService.Verify(
            x => x.SendAsync(
                "customer@test.com",
                It.Is<string>(subject => subject.Contains("iade tamamlandı")),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Once);

        dbContext.InboxMessages.Should().ContainSingle(x =>
            x.ConsumerName == "RefundRequestedConsumer" &&
            x.MessageId == message.EventId);
        VerifyRefundConsumerLogContains(loggerMock, LogLevel.Information, AnalyticsLogSchema.Events.RefundProcessed);
    }

    [Fact]
    public async Task RefundRequestedConsumer_WhenRefundFailsAndRetryScheduled_ShouldSkipFailureEmail()
    {
        await using var dbContext = CreateDbContext();
        var refundService = new Mock<IRefundService>();
        var refundRetryScheduler = new Mock<IRefundRetryScheduler>();

        refundService.Setup(x => x.ProcessRefundAsync(701, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EcommerceAPI.Core.Utilities.Results.ErrorDataResult<RefundRequestDto>(
                new RefundRequestDto
                {
                    Id = 701,
                    ReturnRequestId = 401,
                    OrderId = 301,
                    UserId = 52,
                    OrderNumber = "ORD-301",
                    CustomerEmail = "retry@test.com",
                    CustomerName = "Retry Customer",
                    Amount = 149.90m,
                    Currency = "TRY",
                    Status = RefundRequestStatus.Failed.ToString(),
                    FailureReason = "Gateway temporary error"
                },
                "Gateway temporary error"));

        refundRetryScheduler
            .Setup(x => x.TryScheduleRetry(It.Is<RefundRequestedEvent>(evt =>
                evt.RefundRequestId == 701 &&
                evt.RetryAttempt == 0)))
            .Returns(true);

        var emailService = new Mock<IEmailNotificationService>();
        emailService
            .Setup(x => x.SendAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        var notificationService = new Mock<INotificationService>();
        notificationService
            .Setup(x => x.CreateNotificationAsync(It.IsAny<CreateNotificationRequest>()))
            .ReturnsAsync(new EcommerceAPI.Core.Utilities.Results.SuccessDataResult<NotificationDto>(new NotificationDto()));
        var notificationPreferenceService = new Mock<INotificationPreferenceService>();
        var loggerMock = new Mock<ILogger<RefundRequestedConsumer>>();
        notificationPreferenceService
            .Setup(x => x.GetChannelSettingsAsync(52, NotificationType.Refund))
            .ReturnsAsync(new NotificationChannelSettingsDto
            {
                InAppEnabled = true,
                EmailEnabled = true,
                PushEnabled = false,
                SupportsInApp = true,
                SupportsEmail = true,
                SupportsPush = true
            });

        var consumer = new RefundRequestedConsumer(
            dbContext,
            refundService.Object,
            refundRetryScheduler.Object,
            emailService.Object,
            notificationService.Object,
            notificationPreferenceService.Object,
            loggerMock.Object);

        var message = new RefundRequestedEvent
        {
            EventId = Guid.NewGuid(),
            RefundRequestId = 701,
            ReturnRequestId = 401,
            OrderId = 301,
            UserId = 52,
            Amount = 149.90m,
            Currency = "TRY",
            RetryAttempt = 0
        };

        var context = CreateConsumeContext(message);

        await consumer.Consume(context.Object);

        refundRetryScheduler.Verify(x => x.TryScheduleRetry(It.IsAny<RefundRequestedEvent>()), Times.Once);
        emailService.Verify(
            x => x.SendAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
        notificationService.Verify(
            x => x.CreateNotificationAsync(It.Is<CreateNotificationRequest>(request =>
                request.UserId == 52 &&
                request.Title.Contains("yeniden denenecek"))),
            Times.Once);
        VerifyRefundConsumerLogContains(loggerMock, LogLevel.Warning, AnalyticsLogSchema.Events.RefundFailed);
    }

    private static AppDbContext CreateDbContext()
    {
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        Microsoft.EntityFrameworkCore.InMemoryDbContextOptionsExtensions.UseInMemoryDatabase(
            optionsBuilder,
            Guid.NewGuid().ToString("N"));
        var options = optionsBuilder.Options;

        return new AppDbContext(options);
    }

    private static Mock<ConsumeContext<TMessage>> CreateConsumeContext<TMessage>(TMessage message)
        where TMessage : class
    {
        var context = new Mock<ConsumeContext<TMessage>>();
        context.SetupGet(x => x.Message).Returns(message);
        context.SetupGet(x => x.MessageId).Returns(message switch
        {
            RefundRequestedEvent refundRequested => refundRequested.EventId,
            _ => Guid.NewGuid()
        });
        context.SetupGet(x => x.CancellationToken).Returns(CancellationToken.None);
        return context;
    }

    private static void VerifyRefundConsumerLogContains<T>(Mock<ILogger<T>> loggerMock, LogLevel level, string expectedValue)
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
