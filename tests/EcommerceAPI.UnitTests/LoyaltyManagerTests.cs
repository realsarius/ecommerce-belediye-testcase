using EcommerceAPI.Business.Concrete;
using EcommerceAPI.Core.CrossCuttingConcerns.Logging;
using EcommerceAPI.Core.Interfaces;
using EcommerceAPI.Core.Utilities.Results;
using EcommerceAPI.DataAccess.Abstract;
using EcommerceAPI.Entities.Concrete;
using EcommerceAPI.Entities.Enums;
using FluentAssertions;
using Moq;

namespace EcommerceAPI.UnitTests;

public class LoyaltyManagerTests
{
    private readonly Mock<ILoyaltyTransactionDal> _loyaltyDalMock;
    private readonly Mock<IOrderDal> _orderDalMock;
    private readonly Mock<IUnitOfWork> _uowMock;
    private readonly Mock<IAuditService> _auditServiceMock;
    private readonly LoyaltyManager _loyaltyManager;

    public LoyaltyManagerTests()
    {
        _loyaltyDalMock = new Mock<ILoyaltyTransactionDal>();
        _orderDalMock = new Mock<IOrderDal>();
        _uowMock = new Mock<IUnitOfWork>();
        _auditServiceMock = new Mock<IAuditService>();

        _loyaltyManager = new LoyaltyManager(
            _loyaltyDalMock.Object,
            _orderDalMock.Object,
            _uowMock.Object,
            _auditServiceMock.Object);
    }

    [Fact]
    public async Task CalculateRedemptionAsync_ShouldNormalizePointsAndCapByOrderTotal()
    {
        _loyaltyDalMock.Setup(x => x.GetAvailablePointsAsync(1, It.IsAny<DateTime>()))
            .ReturnsAsync(2800);

        var result = await _loyaltyManager.CalculateRedemptionAsync(1, 2955, 18.40m);

        result.Success.Should().BeTrue();
        result.Data.AppliedPoints.Should().Be(1700);
        result.Data.DiscountAmount.Should().Be(17m);
    }

    [Fact]
    public async Task RedeemPointsForOrderAsync_ShouldCreateNegativeTransactionAndUpdateOrder()
    {
        var order = new Order
        {
            Id = 12,
            UserId = 1,
            OrderNumber = "ORD-12",
            TotalAmount = 120m
        };

        _orderDalMock.Setup(x => x.GetByIdWithDetailsAsync(order.Id)).ReturnsAsync(order);
        _loyaltyDalMock.Setup(x => x.GetByOrderAndTypeAsync(order.Id, LoyaltyTransactionType.Redeemed))
            .ReturnsAsync((LoyaltyTransaction?)null);
        _loyaltyDalMock.Setup(x => x.GetAvailablePointsAsync(order.UserId, It.IsAny<DateTime>()))
            .ReturnsAsync(2500);

        LoyaltyTransaction? captured = null;
        _loyaltyDalMock.Setup(x => x.AddAsync(It.IsAny<LoyaltyTransaction>()))
            .Callback<LoyaltyTransaction>(x => captured = x)
            .ReturnsAsync((LoyaltyTransaction x) => x);

        var result = await _loyaltyManager.RedeemPointsForOrderAsync(1, order.Id, 1500, 15m, "checkout");

        result.Success.Should().BeTrue();
        captured.Should().NotBeNull();
        captured!.Points.Should().Be(-1500);
        captured.BalanceAfter.Should().Be(1000);
        order.LoyaltyPointsUsed.Should().Be(1500);
        order.LoyaltyDiscountAmount.Should().Be(15m);
    }

    [Fact]
    public async Task AwardPointsForOrderAsync_ShouldSetOrderEarnedPointsAndCreateTransaction()
    {
        var order = new Order
        {
            Id = 45,
            UserId = 1,
            OrderNumber = "ORD-45",
            TotalAmount = 349.90m
        };

        _orderDalMock.Setup(x => x.GetByIdWithDetailsAsync(order.Id)).ReturnsAsync(order);
        _loyaltyDalMock.Setup(x => x.GetByOrderAndTypeAsync(order.Id, LoyaltyTransactionType.Earned))
            .ReturnsAsync((LoyaltyTransaction?)null);
        _loyaltyDalMock.Setup(x => x.GetAvailablePointsAsync(order.UserId, It.IsAny<DateTime>()))
            .ReturnsAsync(500);

        LoyaltyTransaction? captured = null;
        _loyaltyDalMock.Setup(x => x.AddAsync(It.IsAny<LoyaltyTransaction>()))
            .Callback<LoyaltyTransaction>(x => captured = x)
            .ReturnsAsync((LoyaltyTransaction x) => x);

        var result = await _loyaltyManager.AwardPointsForOrderAsync(1, order.Id, 349.90m);

        result.Success.Should().BeTrue();
        order.LoyaltyPointsEarned.Should().Be(349);
        captured.Should().NotBeNull();
        captured!.Points.Should().Be(349);
        captured.BalanceAfter.Should().Be(849);
    }
}
