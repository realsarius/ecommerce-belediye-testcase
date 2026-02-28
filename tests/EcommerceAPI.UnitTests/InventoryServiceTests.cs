using FluentAssertions;
using Moq;
using EcommerceAPI.Business.Concrete;
using EcommerceAPI.Business.Abstract;
using EcommerceAPI.Core.Interfaces;
using EcommerceAPI.Core.CrossCuttingConcerns.Logging;
using EcommerceAPI.Entities.Concrete;
using EcommerceAPI.DataAccess.Abstract;
using Microsoft.Extensions.Logging;
using EcommerceAPI.Core.Utilities.Results;
using EcommerceAPI.Entities.IntegrationEvents;
using MassTransit;

namespace EcommerceAPI.UnitTests;

public class InventoryManagerTests
{
    private readonly Mock<IInventoryDal> _inventoryDalMock;
    private readonly Mock<IDistributedLockService> _lockServiceMock;
    private readonly Mock<IAuditService> _auditServiceMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IPublishEndpoint> _publishEndpointMock;
    private readonly Mock<ILogger<InventoryManager>> _loggerMock;
    private readonly InventoryManager _inventoryManager;

    public InventoryManagerTests()
    {
        _inventoryDalMock = new Mock<IInventoryDal>();
        _lockServiceMock = new Mock<IDistributedLockService>();
        _auditServiceMock = new Mock<IAuditService>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _publishEndpointMock = new Mock<IPublishEndpoint>();
        _loggerMock = new Mock<ILogger<InventoryManager>>();

        _lockServiceMock
            .Setup(x => x.ExecuteWithLockAsync(
                It.IsAny<string>(),
                It.IsAny<Func<Task<IResult>>>(),
                It.IsAny<int>()))
            .Returns<string, Func<Task<IResult>>, int>((key, callback, timeout) => callback());

        _publishEndpointMock
            .Setup(x => x.Publish(It.IsAny<WishlistProductLowStockEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        
        _inventoryManager = new InventoryManager(
            _inventoryDalMock.Object,
            _lockServiceMock.Object,
            _auditServiceMock.Object,
            _unitOfWorkMock.Object,
            _publishEndpointMock.Object,
            _loggerMock.Object
        );
    }

    [Fact]
    public async Task DecreaseStockAsync_ValidStock_ShouldDecreaseQuantity()
    {
        var productId = 1;
        var userId = 1;
        var initialStock = 10;
        var decreaseAmount = 2;
        
        var inventory = new Inventory 
        { 
            ProductId = productId, 
            QuantityAvailable = initialStock 
        };

        _inventoryDalMock.Setup(x => x.GetByProductIdsAsync(It.IsAny<List<int>>()))
            .ReturnsAsync(new List<Inventory> { inventory });

        var result = await _inventoryManager.DecreaseStockAsync(productId, decreaseAmount, userId, "Test Reason");

        result.Success.Should().BeTrue();
        inventory.QuantityAvailable.Should().Be(initialStock - decreaseAmount);
        _inventoryDalMock.Verify(x => x.Update(inventory), Times.Once);
    }

    [Fact]
    public async Task IncreaseStockAsync_ValidProduct_ShouldIncreaseQuantity()
    {
        var productId = 1;
        var userId = 1;
        var initialStock = 5;
        var increaseAmount = 3;

        var inventory = new Inventory 
        { 
            ProductId = productId, 
            QuantityAvailable = initialStock 
        };

        _inventoryDalMock.Setup(x => x.GetByProductIdsAsync(It.IsAny<List<int>>()))
            .ReturnsAsync(new List<Inventory> { inventory });

        var result = await _inventoryManager.IncreaseStockAsync(productId, increaseAmount, userId, "Test Return");

        result.Success.Should().BeTrue();
        inventory.QuantityAvailable.Should().Be(initialStock + increaseAmount);
        _inventoryDalMock.Verify(x => x.Update(inventory), Times.Once);
    }

    [Fact]
    public async Task DecreaseStockAsync_WhenThresholdCrossed_ShouldPublishLowStockEvent()
    {
        const int productId = 1;
        const int userId = 1;
        const int initialStock = 8;
        const int decreaseAmount = 4;

        var inventory = new Inventory
        {
            ProductId = productId,
            QuantityAvailable = initialStock
        };

        _inventoryDalMock.Setup(x => x.GetByProductIdsAsync(It.IsAny<List<int>>()))
            .ReturnsAsync(new List<Inventory> { inventory });

        var result = await _inventoryManager.DecreaseStockAsync(productId, decreaseAmount, userId, "Sipariş rezervasyonu");

        result.Success.Should().BeTrue();
        inventory.QuantityAvailable.Should().Be(4);
        _publishEndpointMock.Verify(x => x.Publish(
            It.Is<WishlistProductLowStockEvent>(message =>
                message.ProductId == productId &&
                message.StockQuantity == 4 &&
                message.Threshold == 5),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
