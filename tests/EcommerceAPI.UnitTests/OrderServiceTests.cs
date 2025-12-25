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
    private readonly Mock<ICouponService> _couponServiceMock;
    private readonly OrderManager _orderManager;

    public OrderManagerTests()
    {
        _orderDalMock = new Mock<IOrderDal>();
        _cartDalMock = new Mock<ICartDal>();
        _inventoryServiceMock = new Mock<IInventoryService>();
        _cartServiceMock = new Mock<ICartService>();
        _uowMock = new Mock<IUnitOfWork>();
        _couponServiceMock = new Mock<ICouponService>();

        _orderManager = new OrderManager(
            _orderDalMock.Object,
            _cartDalMock.Object,
            _inventoryServiceMock.Object,
            _cartServiceMock.Object,
            _uowMock.Object,
            _couponServiceMock.Object
        );
    }

    [Fact]
    public async Task CheckoutAsync_ValidCart_ShouldCalculateTotalCorrectly()
    {
        var userId = 100;
        var checkoutRequest = new CheckoutRequest 
        { 
            ShippingAddress = "Test Address",
            PaymentMethod = "CreditCard"
        };
        
        var productWithStock = new Product { Id = 1, Name = "P1", Inventory = new Inventory { QuantityAvailable = 10 } };
        var secondProduct = new Product { Id = 2, Name = "P2", Inventory = new Inventory { QuantityAvailable = 10 } };
        
        var cart = new Cart 
        { 
            UserId = userId, 
            Items = new List<CartItem>
            {
                new() { ProductId = 1, Quantity = 2, PriceSnapshot = 50, Product = productWithStock },
                new() { ProductId = 2, Quantity = 1, PriceSnapshot = 25, Product = secondProduct }
            }
        };

        _cartDalMock.Setup(x => x.GetByUserIdWithItemsAsync(userId)).ReturnsAsync(cart);
        _inventoryServiceMock.Setup(x => x.DecreaseStockAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>()))
            .ReturnsAsync(new SuccessResult());
        _cartServiceMock.Setup(x => x.ClearCartAsync(userId)).ReturnsAsync(new SuccessResult());

        Order capturedOrder = null!;
        _orderDalMock.Setup(x => x.AddAsync(It.IsAny<Order>()))
            .Callback<Order>(o => { o.Id = 1; capturedOrder = o; })
            .ReturnsAsync((Order o) => o);

        _orderDalMock.Setup(x => x.GetByIdWithDetailsAsync(It.IsAny<int>()))
            .ReturnsAsync(() => capturedOrder);

        var result = await _orderManager.CheckoutAsync(userId, checkoutRequest);

        capturedOrder.Should().NotBeNull();
        capturedOrder.TotalAmount.Should().Be(154.90m);
        result.Success.Should().BeTrue();
        result.Data.TotalAmount.Should().Be(154.90m);
        _uowMock.Verify(x => x.SaveChangesAsync(), Times.Once);
    }
}
