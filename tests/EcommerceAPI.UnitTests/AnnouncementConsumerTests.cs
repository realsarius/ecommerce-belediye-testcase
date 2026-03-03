using System.Linq.Expressions;
using EcommerceAPI.API.Consumers;
using EcommerceAPI.Business.Abstract;
using EcommerceAPI.Core.Utilities.Results;
using EcommerceAPI.DataAccess.Concrete.EntityFramework.Contexts;
using EcommerceAPI.Entities.Concrete;
using EcommerceAPI.Entities.DTOs;
using EcommerceAPI.Entities.IntegrationEvents;
using FluentAssertions;
using Hangfire;
using Hangfire.Common;
using Hangfire.States;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace EcommerceAPI.UnitTests;

public class AnnouncementConsumerTests
{
    [Fact]
    public async Task AnnouncementCreatedConsumer_WhenAnnouncementIsScheduled_ShouldEnqueueHangfireJobAndSaveInbox()
    {
        await using var dbContext = CreateDbContext();
        var futureDate = DateTime.UtcNow.AddHours(2);

        var announcementService = new Mock<IAnnouncementService>();
        announcementService
            .Setup(x => x.GetByIdAsync(41))
            .ReturnsAsync(new SuccessDataResult<AnnouncementDto>(new AnnouncementDto
            {
                Id = 41,
                Status = "Scheduled",
                ScheduledAt = futureDate
            }));

        var backgroundJobClient = new Mock<IBackgroundJobClient>();
        backgroundJobClient
            .Setup(x => x.Create(
                It.IsAny<Job>(),
                It.IsAny<IState>()))
            .Returns("job-41");

        var consumer = new AnnouncementCreatedConsumer(
            dbContext,
            announcementService.Object,
            backgroundJobClient.Object,
            Mock.Of<ILogger<AnnouncementCreatedConsumer>>());

        var message = new AnnouncementCreatedEvent
        {
            EventId = Guid.NewGuid(),
            AnnouncementId = 41,
            ScheduledAt = futureDate
        };

        var context = CreateConsumeContext(message);
        await consumer.Consume(context.Object);

        backgroundJobClient.Verify(
            x => x.Create(
                It.Is<Job>(job => job.Type == typeof(IAnnouncementService) && job.Method.Name == nameof(IAnnouncementService.SendAnnouncementAsync)),
                It.Is<ScheduledState>(state => state.EnqueueAt == futureDate)),
            Times.Once);

        announcementService.Verify(x => x.SendAnnouncementAsync(It.IsAny<int>()), Times.Never);

        dbContext.InboxMessages.Should().ContainSingle(x =>
            x.ConsumerName == "AnnouncementCreatedConsumer" &&
            x.MessageId == message.EventId);
    }

    [Fact]
    public async Task AnnouncementCreatedConsumer_WhenAnnouncementIsImmediate_ShouldSendAnnouncementAndSaveInbox()
    {
        await using var dbContext = CreateDbContext();

        var announcementService = new Mock<IAnnouncementService>();
        announcementService
            .Setup(x => x.GetByIdAsync(42))
            .ReturnsAsync(new SuccessDataResult<AnnouncementDto>(new AnnouncementDto
            {
                Id = 42,
                Status = "Processing"
            }));
        announcementService
            .Setup(x => x.SendAnnouncementAsync(42))
            .Returns(Task.CompletedTask);

        var backgroundJobClient = new Mock<IBackgroundJobClient>();

        var consumer = new AnnouncementCreatedConsumer(
            dbContext,
            announcementService.Object,
            backgroundJobClient.Object,
            Mock.Of<ILogger<AnnouncementCreatedConsumer>>());

        var message = new AnnouncementCreatedEvent
        {
            EventId = Guid.NewGuid(),
            AnnouncementId = 42
        };

        var context = CreateConsumeContext(message);
        await consumer.Consume(context.Object);

        announcementService.Verify(x => x.SendAnnouncementAsync(42), Times.Once);
        backgroundJobClient.Verify(
            x => x.Create(
                It.IsAny<Job>(),
                It.IsAny<IState>()),
            Times.Never);

        dbContext.InboxMessages.Should().ContainSingle(x =>
            x.ConsumerName == "AnnouncementCreatedConsumer" &&
            x.MessageId == message.EventId);
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
            AnnouncementCreatedEvent announcementCreated => announcementCreated.EventId,
            _ => Guid.NewGuid()
        });
        context.SetupGet(x => x.CancellationToken).Returns(CancellationToken.None);
        return context;
    }
}
