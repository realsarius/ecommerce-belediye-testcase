using EcommerceAPI.API.Consumers;
using EcommerceAPI.Business.Abstract;
using EcommerceAPI.Core.Interfaces;
using EcommerceAPI.DataAccess.Concrete.EntityFramework.Contexts;
using EcommerceAPI.Entities.Concrete;
using EcommerceAPI.Entities.DTOs;
using EcommerceAPI.Entities.IntegrationEvents;
using FluentAssertions;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace EcommerceAPI.UnitTests;

public class RefundConsumerTests
{
    [Fact]
    public async Task RefundRequestedConsumer_WhenRefundSucceeds_ShouldSendEmailAndSaveInbox()
    {
        await using var dbContext = CreateDbContext();
        var refundService = new Mock<IRefundService>();
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

        var consumer = new RefundRequestedConsumer(
            dbContext,
            refundService.Object,
            emailService.Object,
            Mock.Of<ILogger<RefundRequestedConsumer>>());

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
}
