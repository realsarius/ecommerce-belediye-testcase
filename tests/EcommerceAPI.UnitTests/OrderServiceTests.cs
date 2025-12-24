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
    private readonly Mock<IInventoryService> _inventoryServiceMock;
    private readonly Mock<ICartService> _cartServiceMock;
    private readonly Mock<IUnitOfWork> _uowMock;
    private readonly Mock<ICouponService> _couponServiceMock;

    private readonly OrderManager _orderManager;

    public OrderManagerTests()
    {
        _orderDalMock = new Mock<IOrderDal>();
        _inventoryServiceMock = new Mock<IInventoryService>();
        _cartServiceMock = new Mock<ICartService>();
        _uowMock = new Mock<IUnitOfWork>();
        _couponServiceMock = new Mock<ICouponService>();

        _orderManager = new OrderManager(
            _orderDalMock.Object,
            _inventoryServiceMock.Object,
            _cartServiceMock.Object,
            _uowMock.Object,
            _couponServiceMock.Object
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
        
        // OrderManager artık CartService.GetCartAsync kullanıyor (CartDto döner)
        var cartDto = new CartDto
        {
            Id = userId,
            TotalAmount = 125, // 50*2 + 25*1
            Items = new List<CartItemDto>
            {
                new CartItemDto { ProductId = 1, ProductName = "P1", Quantity = 2, UnitPrice = 50, AvailableStock = 10 },
                new CartItemDto { ProductId = 2, ProductName = "P2", Quantity = 1, UnitPrice = 25, AvailableStock = 10 }
            }
        };

        _cartServiceMock.Setup(x => x.GetCartAsync(userId))
            .ReturnsAsync(new SuccessDataResult<CartDto>(cartDto));
        
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
        // 125 (subtotal) + 29.90 (shipping for orders under 1000 TL) = 154.90
        capturedOrder.TotalAmount.Should().Be(154.90m);
        result.Success.Should().BeTrue();
        result.Data.TotalAmount.Should().Be(154.90m);
        
        _uowMock.Verify(x => x.SaveChangesAsync(), Times.Once);
    }
}

