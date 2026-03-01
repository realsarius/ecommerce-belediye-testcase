using EcommerceAPI.Business.Concrete;
using EcommerceAPI.Core.Interfaces;
using EcommerceAPI.DataAccess.Abstract;
using EcommerceAPI.Entities.Concrete;
using EcommerceAPI.Entities.DTOs;
using EcommerceAPI.Entities.Enums;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace EcommerceAPI.UnitTests;

public class NotificationManagerTests
{
    private readonly Mock<INotificationDal> _notificationDalMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly NotificationManager _manager;

    public NotificationManagerTests()
    {
        _notificationDalMock = new Mock<INotificationDal>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _manager = new NotificationManager(
            _notificationDalMock.Object,
            _unitOfWorkMock.Object,
            Mock.Of<ILogger<NotificationManager>>());
    }

    [Fact]
    public async Task CreateNotificationAsync_WithValidPayload_ShouldPersistNotification()
    {
        _notificationDalMock
            .Setup(x => x.AddAsync(It.IsAny<Notification>()))
            .Callback<Notification>(notification => notification.Id = 91)
            .ReturnsAsync((Notification notification) => notification);

        var result = await _manager.CreateNotificationAsync(new CreateNotificationRequest
        {
            UserId = 42,
            Type = "Wishlist",
            Title = "Fiyat düştü",
            Body = "Ürün hedef fiyatınıza ulaştı.",
            DeepLink = "/products/12"
        });

        result.Success.Should().BeTrue();
        result.Data.Id.Should().Be(91);
        result.Data.Type.Should().Be(NotificationType.Wishlist.ToString());
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task MarkAllAsReadAsync_ShouldDelegateToRepository()
    {
        _notificationDalMock
            .Setup(x => x.MarkAllAsReadAsync(42, It.IsAny<DateTime>()))
            .ReturnsAsync(3);

        var result = await _manager.MarkAllAsReadAsync(42);

        result.Success.Should().BeTrue();
        _notificationDalMock.Verify(x => x.MarkAllAsReadAsync(42, It.IsAny<DateTime>()), Times.Once);
    }
}
