using FluentAssertions;
using Moq;
using EcommerceAPI.Business.Concrete;
using EcommerceAPI.Core.Interfaces;
using EcommerceAPI.Core.CrossCuttingConcerns.Logging;
using EcommerceAPI.Entities.Concrete;
using EcommerceAPI.DataAccess.Abstract;
using EcommerceAPI.Core.Utilities.Results;

namespace EcommerceAPI.UnitTests;

/// <summary>
/// Unit tests for InventoryManager.
/// </summary>
public class InventoryManagerTests
{
    private readonly Mock<IInventoryDal> _inventoryDalMock;
    private readonly Mock<IDistributedLockService> _lockServiceMock;
    private readonly Mock<IAuditService> _auditServiceMock;
    private readonly InventoryManager _inventoryManager;

    public InventoryManagerTests()
    {
        _inventoryDalMock = new Mock<IInventoryDal>();
        _lockServiceMock = new Mock<IDistributedLockService>();
        _auditServiceMock = new Mock<IAuditService>();
        
        _lockServiceMock
            .Setup(x => x.ExecuteWithLockAsync<IResult>(
                It.IsAny<string>(), 
                It.IsAny<Func<Task<IResult>>>(), 
                It.IsAny<int>()))
            .Returns<string, Func<Task<IResult>>, int>(async (key, callback, timeout) => 
            {
                return await callback();
            });
        
        _inventoryManager = new InventoryManager(_inventoryDalMock.Object, _lockServiceMock.Object, _auditServiceMock.Object);
    }

    [Fact]
    public async Task DecreaseStockAsync_ShouldDecreaseQuantity_WhenStockIsAvailable()
    {
        // Arrange
        var productId = 1;
        var userId = 1;
        var initialStock = 10;
        var decreaseAmount = 2;
        
        var inventory = new Inventory 
        { 
            ProductId = productId, 
            QuantityAvailable = initialStock 
        };

        _inventoryDalMock.Setup(x => x.GetByProductIdAsync(productId))
            .ReturnsAsync(inventory);

        // Act
        var result = await _inventoryManager.DecreaseStockAsync(productId, decreaseAmount, userId, "Test Reason");

        // Assert
        result.Success.Should().BeTrue();
        inventory.QuantityAvailable.Should().Be(initialStock - decreaseAmount);
        
        _inventoryDalMock.Verify(x => x.Update(inventory), Times.Once);
        _inventoryDalMock.Verify(x => x.AddMovementAsync(It.Is<InventoryMovement>(m => 
            m.ProductId == productId && 
            m.Delta == -decreaseAmount &&
            m.UserId == userId)), Times.Once);
    }

    [Fact]
    public async Task DecreaseStockAsync_ShouldFail_WhenInsufficientStock()
    {
        // Arrange
        var productId = 1;
        var userId = 1;
        var initialStock = 5;
        var decreaseAmount = 10;
        
        var inventory = new Inventory 
        { 
            ProductId = productId, 
            QuantityAvailable = initialStock 
        };

        _inventoryDalMock.Setup(x => x.GetByProductIdAsync(productId))
            .ReturnsAsync(inventory);

        // Act
        var result = await _inventoryManager.DecreaseStockAsync(productId, decreaseAmount, userId, "Test Reason");

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Stok yetersiz");
        
        inventory.QuantityAvailable.Should().Be(initialStock);
        _inventoryDalMock.Verify(x => x.Update(It.IsAny<Inventory>()), Times.Never);
    }

    [Fact]
    public async Task DecreaseStockAsync_ShouldFail_WhenInventoryNotFound()
    {
        // Arrange
        var productId = 999;
        var userId = 1;
        
        _inventoryDalMock.Setup(x => x.GetByProductIdAsync(productId))
            .ReturnsAsync((Inventory?)null);

        // Act
        var result = await _inventoryManager.DecreaseStockAsync(productId, 5, userId, "Test Reason");

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Stok kaydı bulunamadı");
    }

    [Fact]
    public async Task IncreaseStockAsync_ShouldIncreaseQuantity()
    {
        // Arrange
        var productId = 1;
        var userId = 1;
        var initialStock = 5;
        var increaseAmount = 3;

        var inventory = new Inventory 
        { 
            ProductId = productId, 
            QuantityAvailable = initialStock 
        };

        _inventoryDalMock.Setup(x => x.GetByProductIdAsync(productId))
            .ReturnsAsync(inventory);

        // Act
        var result = await _inventoryManager.IncreaseStockAsync(productId, increaseAmount, userId, "Test Return");

        // Assert
        result.Success.Should().BeTrue();
        inventory.QuantityAvailable.Should().Be(initialStock + increaseAmount);

        _inventoryDalMock.Verify(x => x.Update(inventory), Times.Once);
        _inventoryDalMock.Verify(x => x.AddMovementAsync(It.Is<InventoryMovement>(m => 
            m.ProductId == productId && 
            m.Delta == increaseAmount &&
            m.UserId == userId)), Times.Once);
    }

    [Fact]
    public async Task DecreaseStockAsync_ShouldAcquireLock_WithCorrectKey()
    {
        // Arrange
        var productId = 42;
        var userId = 1;
        
        var inventory = new Inventory 
        { 
            ProductId = productId, 
            QuantityAvailable = 100 
        };

        _inventoryDalMock.Setup(x => x.GetByProductIdAsync(productId))
            .ReturnsAsync(inventory);

        // Act
        await _inventoryManager.DecreaseStockAsync(productId, 5, userId, "Test");

        _lockServiceMock.Verify(x => x.ExecuteWithLockAsync<IResult>(
            $"lock:product:{productId}",
            It.IsAny<Func<Task<IResult>>>(),
            It.IsAny<int>()), Times.Once);
    }
}
