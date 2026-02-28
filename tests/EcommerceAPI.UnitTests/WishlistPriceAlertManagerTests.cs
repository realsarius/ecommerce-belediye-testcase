using EcommerceAPI.Business.Concrete;
using EcommerceAPI.Core.CrossCuttingConcerns.Logging;
using EcommerceAPI.Core.Interfaces;
using EcommerceAPI.DataAccess.Abstract;
using EcommerceAPI.Entities.Concrete;
using EcommerceAPI.Entities.DTOs;
using EcommerceAPI.Entities.IntegrationEvents;
using FluentAssertions;
using MassTransit;
using Microsoft.Extensions.Logging;
using Moq;

namespace EcommerceAPI.UnitTests;

public class WishlistPriceAlertManagerTests
{
    private readonly Mock<IPriceAlertDal> _priceAlertDalMock;
    private readonly Mock<IProductDal> _productDalMock;
    private readonly Mock<IWishlistDal> _wishlistDalMock;
    private readonly Mock<IWishlistItemDal> _wishlistItemDalMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IAuditService> _auditServiceMock;
    private readonly Mock<IPublishEndpoint> _publishEndpointMock;
    private readonly Mock<ILogger<WishlistPriceAlertManager>> _loggerMock;
    private readonly WishlistPriceAlertManager _manager;

    public WishlistPriceAlertManagerTests()
    {
        _priceAlertDalMock = new Mock<IPriceAlertDal>();
        _productDalMock = new Mock<IProductDal>();
        _wishlistDalMock = new Mock<IWishlistDal>();
        _wishlistItemDalMock = new Mock<IWishlistItemDal>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _auditServiceMock = new Mock<IAuditService>();
        _publishEndpointMock = new Mock<IPublishEndpoint>();
        _loggerMock = new Mock<ILogger<WishlistPriceAlertManager>>();

        _publishEndpointMock
            .Setup(x => x.Publish(It.IsAny<WishlistProductPriceDropEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _manager = new WishlistPriceAlertManager(
            _priceAlertDalMock.Object,
            _productDalMock.Object,
            _wishlistDalMock.Object,
            _wishlistItemDalMock.Object,
            _unitOfWorkMock.Object,
            _auditServiceMock.Object,
            _publishEndpointMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task UpsertPriceAlertAsync_WhenWishlistContainsProduct_CreatesAlert()
    {
        const int userId = 7;
        const int productId = 42;
        var product = new Product
        {
            Id = productId,
            Name = "Kulaklık",
            Price = 500m,
            Currency = "TRY",
            IsActive = true
        };

        _productDalMock
            .Setup(x => x.GetAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Product, bool>>>()))
            .ReturnsAsync(product);
        _wishlistDalMock
            .Setup(x => x.GetAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Wishlist, bool>>>()))
            .ReturnsAsync(new Wishlist { Id = 10, UserId = userId });
        _wishlistItemDalMock
            .Setup(x => x.ExistsAsync(It.IsAny<System.Linq.Expressions.Expression<Func<WishlistItem, bool>>>()))
            .ReturnsAsync(true);
        _priceAlertDalMock
            .Setup(x => x.GetByUserAndProductAsync(userId, productId))
            .ReturnsAsync((PriceAlert?)null);

        var result = await _manager.UpsertPriceAlertAsync(userId, new UpsertWishlistPriceAlertRequest
        {
            ProductId = productId,
            TargetPrice = 450m
        });

        result.Success.Should().BeTrue();
        result.Data.ProductId.Should().Be(productId);
        result.Data.TargetPrice.Should().Be(450m);
        _priceAlertDalMock.Verify(x => x.AddAsync(It.Is<PriceAlert>(alert =>
            alert.UserId == userId &&
            alert.ProductId == productId &&
            alert.TargetPrice == 450m &&
            alert.LastKnownPrice == 500m &&
            alert.IsActive)), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task ProcessPriceAlertsAsync_WhenTargetReached_PublishesPriceDropEvent()
    {
        var alert = new PriceAlert
        {
            Id = 1,
            UserId = 3,
            ProductId = 14,
            TargetPrice = 90m,
            LastKnownPrice = 120m,
            IsActive = true,
            Product = new Product
            {
                Id = 14,
                Name = "Klavye",
                Price = 85m,
                Currency = "TRY",
                IsActive = true
            }
        };

        _priceAlertDalMock
            .Setup(x => x.GetActiveAlertsWithProductsAsync())
            .ReturnsAsync(new List<PriceAlert> { alert });

        await _manager.ProcessPriceAlertsAsync();

        _publishEndpointMock.Verify(x => x.Publish(It.Is<WishlistProductPriceDropEvent>(message =>
            message.UserId == 3 &&
            message.ProductId == 14 &&
            message.OldPrice == 120m &&
            message.NewPrice == 85m &&
            message.TargetPrice == 90m), It.IsAny<CancellationToken>()), Times.Once);
        alert.LastKnownPrice.Should().Be(85m);
        alert.LastTriggeredPrice.Should().Be(85m);
        alert.LastNotifiedAt.Should().NotBeNull();
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(), Times.Once);
    }
}
