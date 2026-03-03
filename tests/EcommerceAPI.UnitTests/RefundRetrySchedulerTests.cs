using EcommerceAPI.Business.Abstract;
using EcommerceAPI.Entities.IntegrationEvents;
using EcommerceAPI.Infrastructure.Services;
using EcommerceAPI.Infrastructure.Settings;
using FluentAssertions;
using Hangfire;
using Hangfire.Common;
using Hangfire.States;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace EcommerceAPI.UnitTests;

public class RefundRetrySchedulerTests
{
    [Fact]
    public void TryScheduleRetry_WhenWithinLimit_ShouldCreateHangfireJob()
    {
        var backgroundJobClient = new Mock<IBackgroundJobClient>();
        var createdJobs = new List<(Job Job, IState State)>();

        backgroundJobClient
            .Setup(x => x.Create(It.IsAny<Job>(), It.IsAny<IState>()))
            .Callback<Job, IState>((job, state) => createdJobs.Add((job, state)))
            .Returns("refund-retry-job-1");

        var scheduler = new HangfireRefundRetryScheduler(
            backgroundJobClient.Object,
            Options.Create(new RefundRetrySettings
            {
                Enabled = true,
                MaxAttempts = 3,
                InitialDelayMinutes = 5,
                BackoffMultiplier = 2
            }),
            Mock.Of<ILogger<HangfireRefundRetryScheduler>>());

        var scheduled = scheduler.TryScheduleRetry(new RefundRequestedEvent
        {
            RefundRequestId = 501,
            ReturnRequestId = 301,
            OrderId = 201,
            UserId = 42,
            Amount = 99.90m,
            Currency = "TRY",
            RetryAttempt = 0
        });

        scheduled.Should().BeTrue();
        createdJobs.Should().ContainSingle();
        createdJobs[0].Job.Type.Should().Be(typeof(IRefundRetryJob));
        createdJobs[0].State.Should().BeOfType<ScheduledState>();
        ((ScheduledState)createdJobs[0].State).EnqueueAt.Should().BeAfter(DateTime.UtcNow.AddMinutes(4));
    }
}
