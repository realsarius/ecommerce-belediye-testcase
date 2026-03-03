using System.Reflection;
using System.Text.Json;
using EcommerceAPI.API.Services;
using EcommerceAPI.Entities.Concrete;
using EcommerceAPI.Entities.IntegrationEvents;
using FluentAssertions;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;

namespace EcommerceAPI.UnitTests;

public class OutboxPublisherBackgroundServiceTests
{
    [Fact]
    public async Task ProcessMessageAsync_WhenAnnouncementCreatedEvent_ShouldPublishAndMarkProcessed()
    {
        var publishEndpoint = new Mock<IPublishEndpoint>();
        publishEndpoint
            .Setup(x => x.Publish(It.IsAny<AnnouncementCreatedEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var service = new OutboxPublisherBackgroundService(
            Mock.Of<IServiceScopeFactory>(),
            Mock.Of<ILogger<OutboxPublisherBackgroundService>>());

        var @event = new AnnouncementCreatedEvent
        {
            AnnouncementId = 42,
            ScheduledAt = DateTime.UtcNow.AddMinutes(30)
        };

        var outboxMessage = new OutboxMessage
        {
            EventId = @event.EventId,
            EventType = typeof(AnnouncementCreatedEvent).FullName ?? nameof(AnnouncementCreatedEvent),
            Payload = JsonSerializer.Serialize(@event),
        };

        await InvokeProcessMessageAsync(service, outboxMessage, publishEndpoint.Object);

        publishEndpoint.Verify(
            x => x.Publish(
                It.Is<AnnouncementCreatedEvent>(message =>
                    message.EventId == @event.EventId &&
                    message.AnnouncementId == @event.AnnouncementId),
                It.IsAny<CancellationToken>()),
            Times.Once);

        outboxMessage.ProcessedOnUtc.Should().NotBeNull();
        outboxMessage.RetryCount.Should().Be(0);
        outboxMessage.LastError.Should().BeNull();
    }

    [Fact]
    public async Task ProcessMessageAsync_WhenUnsupportedEvent_ShouldIncreaseRetryAndStoreError()
    {
        var publishEndpoint = new Mock<IPublishEndpoint>();
        var service = new OutboxPublisherBackgroundService(
            Mock.Of<IServiceScopeFactory>(),
            Mock.Of<ILogger<OutboxPublisherBackgroundService>>());

        var outboxMessage = new OutboxMessage
        {
            EventId = Guid.NewGuid(),
            EventType = "Unsupported.Event",
            Payload = "{}",
        };

        await InvokeProcessMessageAsync(service, outboxMessage, publishEndpoint.Object);

        publishEndpoint.Verify(
            x => x.Publish(It.IsAny<object>(), It.IsAny<CancellationToken>()),
            Times.Never);

        outboxMessage.ProcessedOnUtc.Should().BeNull();
        outboxMessage.RetryCount.Should().Be(1);
        outboxMessage.LastError.Should().Contain("Unsupported outbox event type");
    }

    private static async Task InvokeProcessMessageAsync(
        OutboxPublisherBackgroundService service,
        OutboxMessage outboxMessage,
        IPublishEndpoint publishEndpoint)
    {
        var method = typeof(OutboxPublisherBackgroundService).GetMethod(
            "ProcessMessageAsync",
            BindingFlags.Instance | BindingFlags.NonPublic);

        method.Should().NotBeNull();

        var task = method!.Invoke(service, new object[] { outboxMessage, publishEndpoint, CancellationToken.None }) as Task;
        task.Should().NotBeNull();
        await task!;
    }
}
