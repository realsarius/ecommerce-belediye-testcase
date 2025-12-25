using FluentAssertions;
using Moq;
using EcommerceAPI.Business.Concrete;
using EcommerceAPI.Business.Abstract;
using EcommerceAPI.Entities.Concrete;
using EcommerceAPI.Core.Interfaces;
using EcommerceAPI.Core.CrossCuttingConcerns.Logging;
using EcommerceAPI.Entities.DTOs;
using EcommerceAPI.DataAccess.Abstract;
using System.Threading.Tasks;
using System.Collections.Generic;
using EcommerceAPI.Core.Utilities.Results;

namespace EcommerceAPI.UnitTests;

public class OrderManagerTests
{
    private readonly Mock<IOrderDal> _orderDalMock;
    private readonly Mock<IProductDal> _productDalMock;
    private readonly Mock<IInventoryService> _inventoryServiceMock;
    private readonly Mock<ICartService> _cartServiceMock;
    private readonly Mock<IUnitOfWork> _uowMock;
    private readonly Mock<ICouponService> _couponServiceMock;
    private readonly Mock<IAuditService> _auditServiceMock;
    private readonly OrderManager _orderManager;

    public OrderManagerTests()
    {
        _orderDalMock = new Mock<IOrderDal>();
        _productDalMock = new Mock<IProductDal>();
        _inventoryServiceMock = new Mock<IInventoryService>();
        _cartServiceMock = new Mock<ICartService>();
        _uowMock = new Mock<IUnitOfWork>();
        _couponServiceMock = new Mock<ICouponService>();
        _auditServiceMock = new Mock<IAuditService>();

        _orderManager = new OrderManager(
            _orderDalMock.Object,
            _productDalMock.Object,
            _inventoryServiceMock.Object,
            _cartServiceMock.Object,
            _uowMock.Object,
            _couponServiceMock.Object,
            _auditServiceMock.Object
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
        
        var cartDto = new CartDto 
        { 
            Id = 1,
            TotalAmount = 125m,
            Items = new List<CartItemDto>
            {
                new() { ProductId = 1, ProductName = "P1", Quantity = 2, UnitPrice = 50, AvailableStock = 10 },
                new() { ProductId = 2, ProductName = "P2", Quantity = 1, UnitPrice = 25, AvailableStock = 10 }
            }
        };

        _cartServiceMock.Setup(x => x.GetCartAsync(userId))
            .ReturnsAsync(new SuccessDataResult<CartDto>(cartDto));
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
