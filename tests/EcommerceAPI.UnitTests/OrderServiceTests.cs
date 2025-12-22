using FluentAssertions;
using Moq;
using EcommerceAPI.Business.Services.Concrete;
using EcommerceAPI.Business.Services.Abstract;
using EcommerceAPI.Core.Entities;
using EcommerceAPI.Core.Interfaces;
using EcommerceAPI.Core.DTOs;
using System.Threading.Tasks;

namespace EcommerceAPI.UnitTests;

public class OrderServiceTests
{
    private readonly Mock<IOrderRepository> _orderRepo;
    private readonly Mock<ICartRepository> _cartRepo;
    private readonly Mock<IInventoryService> _inventoryService;
    private readonly Mock<ICartService> _cartService;
    private readonly Mock<IUnitOfWork> _uow;

    private readonly OrderService _orderServiceSut;

    public OrderServiceTests()
    {
        _orderRepo = new Mock<IOrderRepository>();
        _cartRepo = new Mock<ICartRepository>();
        _inventoryService = new Mock<IInventoryService>();
        _cartService = new Mock<ICartService>();
        _uow = new Mock<IUnitOfWork>();

        _orderServiceSut = new OrderService(
            _orderRepo.Object,
            _cartRepo.Object,
            _inventoryService.Object,
            _cartService.Object,
            _uow.Object // Assuming UnitOfWork is injected
        );
    }

    [Fact]
    public async Task CheckoutAsync_ShouldCalculateTotalCorrectly()
    {
        // Arrange
        var userId = 100;
        var request = new CheckoutRequest 
        { 
            ShippingAddress = "Test Address",
            PaymentMethod = "CreditCard"
        };
        
        var cart = new Cart { UserId = userId };
        cart.Items.Add(new CartItem { ProductId = 1, Quantity = 2, PriceSnapshot = 50, Product = new Product { Inventory = new Inventory { QuantityAvailable = 10 } } }); // 100
        cart.Items.Add(new CartItem { ProductId = 2, Quantity = 1, PriceSnapshot = 25, Product = new Product { Inventory = new Inventory { QuantityAvailable = 10 } } }); // 25
        // Total should be 125

        _cartRepo.Setup(x => x.GetActiveCartByUserIdAsync(userId)).ReturnsAsync(cart);
        

        // Mock AddAsync to capture the Order
        Order capturedOrder = null!;
        _orderRepo.Setup(x => x.AddAsync(It.IsAny<Order>()))
            .Callback<Order>(o => capturedOrder = o)
            .ReturnsAsync((Order o) => o);

        _orderRepo.Setup(x => x.GetByIdWithDetailsAsync(It.IsAny<int>()))
            .ReturnsAsync(() => capturedOrder); 

        // Act
        var result = await _orderServiceSut.CheckoutAsync(userId, request);

        // Assert
        capturedOrder.Should().NotBeNull();
        capturedOrder.TotalAmount.Should().Be(125);
        result.TotalAmount.Should().Be(125);
    }
}
