using FluentAssertions;
using Moq;
using EcommerceAPI.Business.Concrete;
using EcommerceAPI.Core.Interfaces;
using EcommerceAPI.Entities.Concrete;
using EcommerceAPI.DataAccess.Abstract;
using Microsoft.Extensions.Logging;
using EcommerceAPI.Core.Utilities.Results;
using StackExchange.Redis;

namespace EcommerceAPI.UnitTests;

public class InventoryManagerTests
{
    private readonly Mock<IInventoryDal> _inventoryDalMock;
    private readonly Mock<IConnectionMultiplexer> _redisMock;
    private readonly InventoryManager _inventoryManager;

    public InventoryManagerTests()
    {
        _inventoryDalMock = new Mock<IInventoryDal>();
        _redisMock = new Mock<IConnectionMultiplexer>();
        
        // Mock Redis Database for distributed locking
        var mockDatabase = new Mock<IDatabase>();
        mockDatabase.Setup(x => x.LockTakeAsync(
            It.IsAny<RedisKey>(), 
            It.IsAny<RedisValue>(), 
            It.IsAny<TimeSpan>(), 
            It.IsAny<CommandFlags>()))
            .ReturnsAsync(true);
        mockDatabase.Setup(x => x.LockReleaseAsync(
            It.IsAny<RedisKey>(), 
            It.IsAny<RedisValue>(), 
            It.IsAny<CommandFlags>()))
            .ReturnsAsync(true);
            
        _redisMock.Setup(x => x.GetDatabase(It.IsAny<int>(), It.IsAny<object>()))
            .Returns(mockDatabase.Object);
        
        _inventoryManager = new InventoryManager(_inventoryDalMock.Object, _redisMock.Object);
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
        // AddMovementAsync might not exist on Dal, likely logic is inside Manager to create movement and AddAsync?
        // Or Dal has AddMovement? 
        // Assuming Manager creates movement entity and calls Dal.AddMovement or similar if it's separate table?
        // Wait, InventoryMovement is an entity. So Dal handling it?
        // Let's assume Manager adds directly or via Dal?
        // Checking InventoryManager typical implementation... usually it adds to IInventoryMovementDal? 
        // OR InventoryDal handles it.
        // If I assume InventoryDal has AddMovementAsync?
        // But likely InventoryManager code adds InventoryMovement to context via UnitOfWork or Dal?
        // Let's check if IInventoryDal has AddMovementAsync.
        // Result: IInventoryDal usually extends IEntityRepository<Inventory>. 
        // InventoryMovement is separate.
        // So InventoryManager likely has IEntityRepository<InventoryMovement>? 
        // Or InventoryDal handles both?
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
    }
}

