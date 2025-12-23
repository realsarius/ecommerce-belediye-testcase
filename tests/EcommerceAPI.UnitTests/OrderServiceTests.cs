using FluentAssertions;
using Moq;
using EcommerceAPI.Business.Concrete;
using EcommerceAPI.Business.Abstract;
using EcommerceAPI.Entities.Concrete;
using EcommerceAPI.Core.Interfaces;
using EcommerceAPI.Entities.DTOs;
using EcommerceAPI.DataAccess.Abstract;
using System.Threading.Tasks;
using System.Collections.Generic;
using EcommerceAPI.Core.Utilities.Results;

namespace EcommerceAPI.UnitTests;

public class OrderManagerTests
{
    private readonly Mock<IOrderDal> _orderDalMock;
    private readonly Mock<ICartDal> _cartDalMock;
    private readonly Mock<IInventoryService> _inventoryServiceMock;
    private readonly Mock<ICartService> _cartServiceMock;
    private readonly Mock<IUnitOfWork> _uowMock;
    private readonly Mock<IPaymentService> _paymentServiceMock; // Added as it might be needed if constructor changes, though current OrderManager doesn't seem to use it in ctor shown above?
    // Wait, viewing OrderManager above: 
    // public OrderManager(IOrderDal orderDal, ICartDal cartDal, IInventoryService inventoryService, ICartService cartService, IUnitOfWork unitOfWork)
    // It DOES NOT have IPaymentService in constructor in the file I viewed.

    private readonly OrderManager _orderManager;

    public OrderManagerTests()
    {
        _orderDalMock = new Mock<IOrderDal>();
        _cartDalMock = new Mock<ICartDal>();
        _inventoryServiceMock = new Mock<IInventoryService>();
        _cartServiceMock = new Mock<ICartService>();
        _uowMock = new Mock<IUnitOfWork>();

        _orderManager = new OrderManager(
            _orderDalMock.Object,
            _cartDalMock.Object,
            _inventoryServiceMock.Object,
            _cartServiceMock.Object,
            _uowMock.Object
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
        
        var cart = new Cart { UserId = userId, Items = new List<CartItem>() };
        // Product setup for inventory check
        var prod1 = new Product { Id = 1, Name = "P1", Inventory = new Inventory { QuantityAvailable = 10 } };
        var prod2 = new Product { Id = 2, Name = "P2", Inventory = new Inventory { QuantityAvailable = 10 } };

        cart.Items.Add(new CartItem { ProductId = 1, Quantity = 2, PriceSnapshot = 50, Product = prod1 }); // 100
        cart.Items.Add(new CartItem { ProductId = 2, Quantity = 1, PriceSnapshot = 25, Product = prod2 }); // 25
        // Total should be 125

        _cartDalMock.Setup(x => x.GetByUserIdWithItemsAsync(userId)).ReturnsAsync(cart);
        
        // Mock InventoryService DecreaseStock
        _inventoryServiceMock.Setup(x => x.DecreaseStockAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>()))
            .ReturnsAsync(new SuccessResult());

        // Mock CartService ClearCart
        _cartServiceMock.Setup(x => x.ClearCartAsync(userId)).ReturnsAsync(new SuccessResult());

        // Mock AddAsync to capture the Order
        Order capturedOrder = null!;
        _orderDalMock.Setup(x => x.AddAsync(It.IsAny<Order>()))
            .Callback<Order>(o => {
                o.Id = 1; // Simulate DB ID generation
                capturedOrder = o;
            })
            .ReturnsAsync((Order o) => o);

        // GetByIdWithDetailsAsync is called at the end of CheckoutAsync to return the DTO
        _orderDalMock.Setup(x => x.GetByIdWithDetailsAsync(It.IsAny<int>()))
            .ReturnsAsync(() => capturedOrder); 

        // Act
        var result = await _orderManager.CheckoutAsync(userId, request);

        // Assert
        capturedOrder.Should().NotBeNull();
        capturedOrder.TotalAmount.Should().Be(125);
        result.Success.Should().BeTrue();
        result.Data.TotalAmount.Should().Be(125);
        
        _uowMock.Verify(x => x.SaveChangesAsync(), Times.Once);
    }
}

