using FluentAssertions;
using Moq;
using EcommerceAPI.Business.Concrete;
using EcommerceAPI.Core.Interfaces;
using EcommerceAPI.Entities.Concrete;
using EcommerceAPI.DataAccess.Abstract;
using Microsoft.Extensions.Logging;
using EcommerceAPI.Core.Utilities.Results;

namespace EcommerceAPI.UnitTests;

public class InventoryManagerTests
{
    private readonly Mock<IInventoryDal> _inventoryDalMock;
    private readonly Mock<IUnitOfWork> _uowMock;
    private readonly Mock<ILogger<InventoryManager>> _loggerMock;
    private readonly InventoryManager _inventoryManager;

    public InventoryManagerTests()
    {
        _inventoryDalMock = new Mock<IInventoryDal>();
        _uowMock = new Mock<IUnitOfWork>();
        _loggerMock = new Mock<ILogger<InventoryManager>>();
        
        _inventoryManager = new InventoryManager(_inventoryDalMock.Object);
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

        _inventoryDalMock.Setup(x => x.GetByProductIdAsync(productId))
            .ReturnsAsync(inventory);

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

        _inventoryDalMock.Setup(x => x.GetByProductIdAsync(productId))
            .ReturnsAsync(inventory);

        var result = await _inventoryManager.IncreaseStockAsync(productId, increaseAmount, userId, "Test Return");

        result.Success.Should().BeTrue();
        inventory.QuantityAvailable.Should().Be(initialStock + increaseAmount);
        _inventoryDalMock.Verify(x => x.Update(inventory), Times.Once);
    }
}
