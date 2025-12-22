using FluentAssertions;
using Moq;
using EcommerceAPI.Business.Services.Concrete;
using EcommerceAPI.Core.Interfaces;
using EcommerceAPI.Core.Entities;

namespace EcommerceAPI.UnitTests;

public class InventoryServiceTests
{
    private readonly Mock<IInventoryRepository> _inventoryRepositoryMock;
    private readonly InventoryService _inventoryService;

    public InventoryServiceTests()
    {
        _inventoryRepositoryMock = new Mock<IInventoryRepository>();
        _inventoryService = new InventoryService(_inventoryRepositoryMock.Object);
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

        _inventoryRepositoryMock.Setup(x => x.GetByProductIdAsync(productId))
            .ReturnsAsync(inventory);

        // Act
        await _inventoryService.DecreaseStockAsync(productId, decreaseAmount, userId, "Test Reason");

        // Assert
        inventory.QuantityAvailable.Should().Be(initialStock - decreaseAmount);
        
        _inventoryRepositoryMock.Verify(x => x.Update(inventory), Times.Once);
        _inventoryRepositoryMock.Verify(x => x.AddMovementAsync(It.Is<InventoryMovement>(m => 
            m.ProductId == productId && 
            m.Delta == -decreaseAmount &&
            m.UserId == userId
        )), Times.Once);
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

        _inventoryRepositoryMock.Setup(x => x.GetByProductIdAsync(productId))
            .ReturnsAsync(inventory);

        // Act
        await _inventoryService.IncreaseStockAsync(productId, increaseAmount, userId, "Test Return");

        // Assert
        inventory.QuantityAvailable.Should().Be(initialStock + increaseAmount);

        _inventoryRepositoryMock.Verify(x => x.Update(inventory), Times.Once);
        _inventoryRepositoryMock.Verify(x => x.AddMovementAsync(It.Is<InventoryMovement>(m => 
            m.ProductId == productId && 
            m.Delta == increaseAmount
        )), Times.Once);
    }
}
