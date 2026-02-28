using EcommerceAPI.Business.Concrete;
using EcommerceAPI.Business.Constants;
using EcommerceAPI.Business.Mappers;
using EcommerceAPI.Core.CrossCuttingConcerns.Logging;
using EcommerceAPI.Core.Interfaces;
using EcommerceAPI.DataAccess.Abstract;
using EcommerceAPI.Entities.Concrete;
using EcommerceAPI.Entities.IntegrationEvents;
using MassTransit;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using System.Linq.Expressions;

namespace EcommerceAPI.Business.Tests;

[TestFixture]
public class WishlistManagerTests
{
    private Mock<IWishlistDal> _mockWishlistDal = null!;
    private Mock<IWishlistItemDal> _mockWishlistItemDal = null!;
    private Mock<IProductDal> _mockProductDal = null!;
    private Mock<IWishlistMapper> _mockWishlistMapper = null!;
    private Mock<IUnitOfWork> _mockUnitOfWork = null!;
    private Mock<IAuditService> _mockAuditService = null!;
    private Mock<ILogger<WishlistManager>> _mockLogger = null!;
    private Mock<IPublishEndpoint> _mockPublishEndpoint = null!;
    private WishlistManager _wishlistManager = null!;

    [SetUp]
    public void Setup()
    {
        _mockWishlistDal = new Mock<IWishlistDal>();
        _mockWishlistItemDal = new Mock<IWishlistItemDal>();
        _mockProductDal = new Mock<IProductDal>();
        _mockWishlistMapper = new Mock<IWishlistMapper>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockAuditService = new Mock<IAuditService>();
        _mockLogger = new Mock<ILogger<WishlistManager>>();
        _mockPublishEndpoint = new Mock<IPublishEndpoint>();
        _mockPublishEndpoint
            .Setup(x => x.Publish(It.IsAny<WishlistItemAddedEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _mockPublishEndpoint
            .Setup(x => x.Publish(It.IsAny<WishlistItemRemovedEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _wishlistManager = new WishlistManager(
            _mockWishlistDal.Object,
            _mockWishlistItemDal.Object,
            _mockProductDal.Object,
            _mockWishlistMapper.Object,
            _mockUnitOfWork.Object,
            _mockAuditService.Object,
            _mockLogger.Object,
            _mockPublishEndpoint.Object
        );
    }

    [Test]
    public async Task AddItemToWishlistAsync_ProductNotFound_ReturnsError()
    {
        _mockProductDal.Setup(d => d.GetAsync(It.IsAny<Expression<Func<Product, bool>>>()))
            .ReturnsAsync((Product?)null);

        var result = await _wishlistManager.AddItemToWishlistAsync(1, 99);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Message, Is.EqualTo(Messages.ProductNotFound));
    }

    [Test]
    public async Task AddItemToWishlistAsync_Success_AddsItem()
    {
        var product = new Product { Id = 1, Price = 120m, IsActive = true };

        _mockProductDal.SetupSequence(d => d.GetAsync(It.IsAny<Expression<Func<Product, bool>>>()))
            .ReturnsAsync(product)
            .ReturnsAsync(product);

        _mockWishlistDal.Setup(d => d.GetOrCreateByUserIdAsync(1))
            .ReturnsAsync(new Wishlist { Id = 1, UserId = 1 });

        _mockWishlistItemDal.SetupSequence(d => d.CountAsync(It.IsAny<Expression<Func<WishlistItem, bool>>>()))
            .ReturnsAsync(0)
            .ReturnsAsync(1);

        _mockWishlistItemDal.Setup(d => d.AddIfNotExistsAsync(It.IsAny<WishlistItem>()))
            .ReturnsAsync(true);

        var result = await _wishlistManager.AddItemToWishlistAsync(1, 1);

        Assert.That(result.Success, Is.True);
        _mockWishlistItemDal.Verify(d => d.AddIfNotExistsAsync(It.Is<WishlistItem>(w => w.ProductId == 1 && w.WishlistId == 1)), Times.Once);
    }
}
