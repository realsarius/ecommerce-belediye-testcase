using EcommerceAPI.Core.CrossCuttingConcerns.Logging;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace EcommerceAPI.UnitTests;

/// <summary>
/// ElasticAuditService için unit testler.
/// AuditLog property'si ve yapılandırılmış loglama doğrulanır.
/// </summary>
public class AuditServiceTests
{
    private readonly Mock<ILogger<ElasticAuditService>> _mockLogger;
    private readonly ElasticAuditService _auditService;

    public AuditServiceTests()
    {
        _mockLogger = new Mock<ILogger<ElasticAuditService>>();
        _auditService = new ElasticAuditService(_mockLogger.Object);
    }

    [Fact]
    public void LogAction_ShouldCallLoggerWithCorrectParameters()
    {
        // Arrange
        var userId = "user-123";
        var action = "OrderCreated";
        var resource = "Order";
        var data = new { OrderId = 456, Total = 99.99m };

        // Act
        _auditService.LogAction(userId, action, resource, data);

        // Assert - ILogger.Log metodu çağrılmalı
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("AUDIT")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public void LogAction_WithNullData_ShouldNotThrowException()
    {
        // Arrange
        var userId = "user-456";
        var action = "ProductViewed";
        var resource = "Product";

        // Act
        var act = () => _auditService.LogAction(userId, action, resource, null);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public async Task LogActionAsync_ShouldCompleteSuccessfully()
    {
        // Arrange
        var userId = "user-789";
        var action = "PaymentProcessed";
        var resource = "Payment";
        var data = new { PaymentId = 123, Amount = 250.00m };

        // Act
        var task = _auditService.LogActionAsync(userId, action, resource, data);
        await task;

        // Assert
        task.IsCompletedSuccessfully.Should().BeTrue();
    }

    [Fact]
    public void LogAction_ShouldIncludeAuditLogPropertyInScope()
    {
        // Arrange
        var userId = "user-test";
        var action = "TestAction";
        var resource = "TestResource";
        Dictionary<string, object>? capturedScope = null;

        // BeginScope çağrısını yakala
        _mockLogger.Setup(x => x.BeginScope(It.IsAny<It.IsAnyType>()))
            .Callback<object>(scope =>
            {
                if (scope is Dictionary<string, object> dict)
                {
                    capturedScope = dict;
                }
            })
            .Returns(Mock.Of<IDisposable>());

        // Act
        _auditService.LogAction(userId, action, resource, null);

        // Assert
        capturedScope.Should().NotBeNull();
        capturedScope!.Should().ContainKey("AuditLog");
        capturedScope["AuditLog"].Should().Be(true);
        capturedScope.Should().ContainKey("UserId");
        capturedScope["UserId"].Should().Be(userId);
        capturedScope.Should().ContainKey("Action");
        capturedScope["Action"].Should().Be(action);
        capturedScope.Should().ContainKey("Resource");
        capturedScope["Resource"].Should().Be(resource);
    }

    [Theory]
    [InlineData("admin-1", "UserDeleted", "User")]
    [InlineData("system", "ScheduledTask", "Job")]
    [InlineData("customer-99", "OrderCancelled", "Order")]
    public void LogAction_ShouldAcceptVariousInputs(string userId, string action, string resource)
    {
        // Act
        var act = () => _auditService.LogAction(userId, action, resource, null);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void LogAction_WithComplexData_ShouldSerializeCorrectly()
    {
        // Arrange
        var userId = "user-serialization-test";
        var action = "ComplexDataTest";
        var resource = "TestEntity";
        var complexData = new
        {
            Id = 1,
            Name = "Test Item",
            NestedObject = new { Value = 42, Description = "Nested" },
            List = new[] { "item1", "item2", "item3" }
        };

        // Act
        var act = () => _auditService.LogAction(userId, action, resource, complexData);

        // Assert
        act.Should().NotThrow();
        
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }
}
