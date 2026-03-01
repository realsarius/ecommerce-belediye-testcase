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
using Microsoft.Extensions.Logging;
using MassTransit;
using EcommerceAPI.Entities.IntegrationEvents;
using Microsoft.EntityFrameworkCore;

namespace EcommerceAPI.UnitTests;

public class OrderManagerTests
{
    private readonly Mock<IOrderDal> _orderDalMock;
    private readonly Mock<IProductDal> _productDalMock;
    private readonly Mock<IInventoryService> _inventoryServiceMock;
    private readonly Mock<ICartService> _cartServiceMock;
    private readonly Mock<IUnitOfWork> _uowMock;
    private readonly Mock<ICouponService> _couponServiceMock;
    private readonly Mock<ILoyaltyService> _loyaltyServiceMock;
    private readonly Mock<IAuditService> _auditServiceMock;
    private readonly Mock<ILogger<OrderManager>> _loggerMock;
    private readonly Mock<IPublishEndpoint> _publishEndpointMock;
    private readonly OrderManager _orderManager;

    public OrderManagerTests()
    {
        _orderDalMock = new Mock<IOrderDal>();
        _productDalMock = new Mock<IProductDal>();
        _inventoryServiceMock = new Mock<IInventoryService>();
        _cartServiceMock = new Mock<ICartService>();
        _uowMock = new Mock<IUnitOfWork>();
        _couponServiceMock = new Mock<ICouponService>();
        _loyaltyServiceMock = new Mock<ILoyaltyService>();
        _auditServiceMock = new Mock<IAuditService>();
        _loggerMock = new Mock<ILogger<OrderManager>>();
        _publishEndpointMock = new Mock<IPublishEndpoint>();
        _publishEndpointMock
            .Setup(x => x.Publish(It.IsAny<OrderCreatedEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _loyaltyServiceMock
            .Setup(x => x.CalculateRedemptionAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<decimal>()))
            .ReturnsAsync(new SuccessDataResult<LoyaltyRedemptionPreviewDto>(new LoyaltyRedemptionPreviewDto()));
        _loyaltyServiceMock
            .Setup(x => x.RedeemPointsForOrderAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<decimal>(), It.IsAny<string>()))
            .ReturnsAsync(new SuccessResult());
        _loyaltyServiceMock
            .Setup(x => x.RestoreRedeemedPointsAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>()))
            .ReturnsAsync(new SuccessResult());

        _orderManager = new OrderManager(
            _orderDalMock.Object,
            _productDalMock.Object,
            _inventoryServiceMock.Object,
            _cartServiceMock.Object,
            _uowMock.Object,
            _couponServiceMock.Object,
            _loyaltyServiceMock.Object,
            _auditServiceMock.Object,
            _loggerMock.Object,
            _publishEndpointMock.Object
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
        
        _inventoryServiceMock.Setup(x => x.ReserveStocksAsync(It.IsAny<Dictionary<int, int>>(), It.IsAny<int>(), It.IsAny<string>()))
            .ReturnsAsync(new SuccessResult());
            
        _couponServiceMock.Setup(x => x.ValidateCouponAsync(It.IsAny<string>(), It.IsAny<decimal>()))
            .ReturnsAsync(new SuccessDataResult<CouponValidationResult>(new CouponValidationResult { IsValid = true, DiscountAmount = 0 }));

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
        _uowMock.Verify(x => x.SaveChangesAsync(), Times.Exactly(2));
        _uowMock.Verify(x => x.CommitTransactionAsync(), Times.Once);
        _cartServiceMock.Verify(x => x.ClearCartAsync(userId), Times.Once);
        _publishEndpointMock.Verify(x => x.Publish(It.IsAny<OrderCreatedEvent>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CheckoutAsync_WithLoyaltyPoints_ShouldApplyDiscountAndRedeemPoints()
    {
        var userId = 100;
        var checkoutRequest = new CheckoutRequest
        {
            ShippingAddress = "Test Address",
            PaymentMethod = "CreditCard",
            LoyaltyPointsToUse = 1500
        };

        var cartDto = new CartDto
        {
            Id = 1,
            TotalAmount = 200m,
            Items = new List<CartItemDto>
            {
                new() { ProductId = 1, ProductName = "P1", Quantity = 2, UnitPrice = 100, AvailableStock = 10 }
            }
        };

        _cartServiceMock.Setup(x => x.GetCartAsync(userId))
            .ReturnsAsync(new SuccessDataResult<CartDto>(cartDto));
        _inventoryServiceMock.Setup(x => x.ReserveStocksAsync(It.IsAny<Dictionary<int, int>>(), It.IsAny<int>(), It.IsAny<string>()))
            .ReturnsAsync(new SuccessResult());
        _couponServiceMock.Setup(x => x.ValidateCouponAsync(It.IsAny<string>(), It.IsAny<decimal>()))
            .ReturnsAsync(new SuccessDataResult<CouponValidationResult>(new CouponValidationResult { IsValid = true, DiscountAmount = 0 }));
        _cartServiceMock.Setup(x => x.ClearCartAsync(userId)).ReturnsAsync(new SuccessResult());
        _loyaltyServiceMock
            .Setup(x => x.CalculateRedemptionAsync(userId, 1500, 229.90m))
            .ReturnsAsync(new SuccessDataResult<LoyaltyRedemptionPreviewDto>(new LoyaltyRedemptionPreviewDto
            {
                RequestedPoints = 1500,
                AppliedPoints = 1500,
                DiscountAmount = 15m,
                AvailablePoints = 2000
            }));
        _loyaltyServiceMock
            .Setup(x => x.RedeemPointsForOrderAsync(userId, It.IsAny<int>(), 1500, 15m, It.IsAny<string>()))
            .ReturnsAsync(new SuccessResult());

        Order capturedOrder = null!;
        _orderDalMock.Setup(x => x.AddAsync(It.IsAny<Order>()))
            .Callback<Order>(o => { o.Id = 101; capturedOrder = o; })
            .ReturnsAsync((Order o) => o);
        _orderDalMock.Setup(x => x.GetByIdWithDetailsAsync(It.IsAny<int>()))
            .ReturnsAsync(() => capturedOrder);

        var result = await _orderManager.CheckoutAsync(userId, checkoutRequest);

        result.Success.Should().BeTrue();
        capturedOrder.TotalAmount.Should().Be(214.90m);
        capturedOrder.LoyaltyPointsUsed.Should().Be(1500);
        capturedOrder.LoyaltyDiscountAmount.Should().Be(15m);
        _loyaltyServiceMock.Verify(x => x.RedeemPointsForOrderAsync(userId, capturedOrder.Id, 1500, 15m, It.IsAny<string>()), Times.Once);
        _uowMock.Verify(x => x.SaveChangesAsync(), Times.Exactly(3));
    }

    [Fact]
    public async Task CheckoutAsync_WhenReserveFails_ShouldReturnError()
    {
        var userId = 100;
        var checkoutRequest = new CheckoutRequest();
        var cartDto = new CartDto 
        { 
            Items = new List<CartItemDto> { new() { ProductId = 1, Quantity = 1, AvailableStock = 10 } } 
        };

        _cartServiceMock.Setup(x => x.GetCartAsync(userId)).ReturnsAsync(new SuccessDataResult<CartDto>(cartDto));
        _couponServiceMock.Setup(x => x.ValidateCouponAsync(It.IsAny<string>(), It.IsAny<decimal>()))
            .ReturnsAsync(new SuccessDataResult<CouponValidationResult>(new CouponValidationResult { IsValid = true }));

        _inventoryServiceMock.Setup(x => x.ReserveStocksAsync(It.IsAny<Dictionary<int, int>>(), It.IsAny<int>(), It.IsAny<string>()))
            .ReturnsAsync(new ErrorResult("Out of stock"));

        var result = await _orderManager.CheckoutAsync(userId, checkoutRequest);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Stok rezervasyon hatası");
        _uowMock.Verify(x => x.RollbackTransactionAsync(), Times.Once);
        _uowMock.Verify(x => x.CommitTransactionAsync(), Times.Never);
        _orderDalMock.Verify(x => x.AddAsync(It.IsAny<Order>()), Times.Never);
    }

    [Fact]
    public async Task CheckoutAsync_WhenDbFails_ShouldRollback()
    {
        var userId = 100;
        var cartDto = new CartDto 
        { 
            Items = new List<CartItemDto> { new() { ProductId = 1, Quantity = 1, AvailableStock = 10 } } 
        };

        _cartServiceMock.Setup(x => x.GetCartAsync(userId)).ReturnsAsync(new SuccessDataResult<CartDto>(cartDto));
        _couponServiceMock.Setup(x => x.ValidateCouponAsync(It.IsAny<string>(), It.IsAny<decimal>()))
            .ReturnsAsync(new SuccessDataResult<CouponValidationResult>(new CouponValidationResult { IsValid = true }));
        
        _inventoryServiceMock.Setup(x => x.ReserveStocksAsync(It.IsAny<Dictionary<int, int>>(), It.IsAny<int>(), It.IsAny<string>()))
            .ReturnsAsync(new SuccessResult());


        _orderDalMock.Setup(x => x.AddAsync(It.IsAny<Order>()))
            .ThrowsAsync(new Exception("DB Connection Failed"));

        var result = await _orderManager.CheckoutAsync(userId, new CheckoutRequest());

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Sipariş oluşturulamadı");
        _uowMock.Verify(x => x.RollbackTransactionAsync(), Times.Once);

        _inventoryServiceMock.Verify(x => x.ReleaseStocksAsync(It.IsAny<Dictionary<int, int>>(), It.IsAny<int>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task CheckoutAsync_WithExistingIdempotencyKey_ShouldReturnExistingOrder()
    {
        var userId = 100;
        const string idempotencyKey = "checkout-key-1";

        var existingOrder = new Order
        {
            Id = 45,
            UserId = userId,
            OrderNumber = "ORD-EXISTING",
            ShippingAddress = "Test Address 123",
            TotalAmount = 249.90m,
            Payment = new Payment
            {
                Id = 9,
                Amount = 249.90m,
                PaymentMethod = "CreditCard",
                IdempotencyKey = idempotencyKey
            }
        };

        _orderDalMock.Setup(x => x.GetAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Order, bool>>>()))
            .ReturnsAsync(existingOrder);
        _orderDalMock.Setup(x => x.GetByIdWithDetailsAsync(existingOrder.Id))
            .ReturnsAsync(existingOrder);

        var result = await _orderManager.CheckoutAsync(userId, new CheckoutRequest
        {
            ShippingAddress = "Test Address 123",
            PaymentMethod = "CreditCard",
            IdempotencyKey = idempotencyKey
        });

        result.Success.Should().BeTrue();
        result.Data.Id.Should().Be(existingOrder.Id);
        _orderDalMock.Verify(x => x.AddAsync(It.IsAny<Order>()), Times.Never);
        _publishEndpointMock.Verify(x => x.Publish(It.IsAny<OrderCreatedEvent>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CheckoutAsync_WhenPersistenceConflictHitsSameIdempotencyKey_ShouldReturnExistingOrder()
    {
        var userId = 100;
        const string idempotencyKey = "checkout-key-race";

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

        var existingOrder = new Order
        {
            Id = 46,
            UserId = userId,
            OrderNumber = "ORD-RACE",
            ShippingAddress = "Test Address 123",
            TotalAmount = 154.90m,
            Payment = new Payment
            {
                Id = 10,
                Amount = 154.90m,
                PaymentMethod = "CreditCard",
                IdempotencyKey = idempotencyKey
            }
        };

        _orderDalMock.SetupSequence(x => x.GetAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Order, bool>>>()))
            .ReturnsAsync((Order?)null)
            .ReturnsAsync(existingOrder);
        _orderDalMock.Setup(x => x.GetByIdWithDetailsAsync(existingOrder.Id))
            .ReturnsAsync(existingOrder);

        _cartServiceMock.Setup(x => x.GetCartAsync(userId))
            .ReturnsAsync(new SuccessDataResult<CartDto>(cartDto));
        _inventoryServiceMock.Setup(x => x.ReserveStocksAsync(It.IsAny<Dictionary<int, int>>(), It.IsAny<int>(), It.IsAny<string>()))
            .ReturnsAsync(new SuccessResult());
        _couponServiceMock.Setup(x => x.ValidateCouponAsync(It.IsAny<string>(), It.IsAny<decimal>()))
            .ReturnsAsync(new SuccessDataResult<CouponValidationResult>(new CouponValidationResult { IsValid = true, DiscountAmount = 0 }));

        _orderDalMock.Setup(x => x.AddAsync(It.IsAny<Order>()))
            .ThrowsAsync(new DbUpdateException("duplicate key", new Exception("duplicate key")));

        var result = await _orderManager.CheckoutAsync(userId, new CheckoutRequest
        {
            ShippingAddress = "Test Address 123",
            PaymentMethod = "CreditCard",
            IdempotencyKey = idempotencyKey
        });

        result.Success.Should().BeTrue();
        result.Data.Id.Should().Be(existingOrder.Id);
        _uowMock.Verify(x => x.RollbackTransactionAsync(), Times.Once);
    }
}
