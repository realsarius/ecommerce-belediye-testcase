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

public class ReferralManagerTests
{
    private readonly Mock<IReferralCodeDal> _referralCodeDalMock;
    private readonly Mock<IReferralTransactionDal> _referralTransactionDalMock;
    private readonly Mock<IUserDal> _userDalMock;
    private readonly Mock<IOrderDal> _orderDalMock;
    private readonly Mock<ILoyaltyTransactionDal> _loyaltyTransactionDalMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IAuditService> _auditServiceMock;
    private readonly ReferralManager _manager;

    public ReferralManagerTests()
    {
        _referralCodeDalMock = new Mock<IReferralCodeDal>();
        _referralTransactionDalMock = new Mock<IReferralTransactionDal>();
        _userDalMock = new Mock<IUserDal>();
        _orderDalMock = new Mock<IOrderDal>();
        _loyaltyTransactionDalMock = new Mock<ILoyaltyTransactionDal>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _auditServiceMock = new Mock<IAuditService>();

        _manager = new ReferralManager(
            _referralCodeDalMock.Object,
            _referralTransactionDalMock.Object,
            _userDalMock.Object,
            _orderDalMock.Object,
            _loyaltyTransactionDalMock.Object,
            _unitOfWorkMock.Object,
            _auditServiceMock.Object);
    }

    [Fact]
    public async Task SetupNewUserAsync_WithValidReferralCode_ShouldAttachReferrerAndCreateSignupTransaction()
    {
        var user = new User
        {
            Id = 10,
            Email = "new@test.com",
            EmailHash = "hash",
            PasswordHash = "pw",
            FirstName = "New",
            LastName = "User",
            RoleId = 2
        };

        var referrerCode = new ReferralCode
        {
            Id = 99,
            UserId = 5,
            Code = "REFTEST01",
            IsActive = true
        };

        _userDalMock.Setup(x => x.GetAsync(It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>()))
            .ReturnsAsync(user);
        _referralCodeDalMock.Setup(x => x.GetByUserIdAsync(user.Id))
            .ReturnsAsync((ReferralCode?)null);
        _userDalMock.Setup(x => x.ExistsAsync(It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>()))
            .ReturnsAsync(true);
        _referralCodeDalMock.Setup(x => x.ExistsAsync(It.IsAny<System.Linq.Expressions.Expression<Func<ReferralCode, bool>>>()))
            .ReturnsAsync(false);
        _referralCodeDalMock.Setup(x => x.GetByCodeAsync("REFTEST01"))
            .ReturnsAsync(referrerCode);
        _referralTransactionDalMock.Setup(x => x.ExistsAsync(It.IsAny<System.Linq.Expressions.Expression<Func<ReferralTransaction, bool>>>()))
            .ReturnsAsync(false);

        ReferralTransaction? capturedTransaction = null;
        _referralTransactionDalMock.Setup(x => x.AddAsync(It.IsAny<ReferralTransaction>()))
            .Callback<ReferralTransaction>(x => capturedTransaction = x)
            .ReturnsAsync((ReferralTransaction x) => x);

        var result = await _manager.SetupNewUserAsync(user.Id, "reftest01");

        result.Success.Should().BeTrue();
        user.ReferredByUserId.Should().Be(referrerCode.UserId);
        user.AppliedReferralCodeId.Should().Be(referrerCode.Id);
        capturedTransaction.Should().NotBeNull();
        capturedTransaction!.Type.Should().Be(ReferralTransactionType.Signup);
        capturedTransaction.ReferrerUserId.Should().Be(referrerCode.UserId);
        capturedTransaction.ReferredUserId.Should().Be(user.Id);
    }

    [Fact]
    public async Task AwardFirstPurchaseRewardsAsync_OnFirstPaidOrder_ShouldCreateRewardTransactions()
    {
        var user = new User
        {
            Id = 10,
            Email = "new@test.com",
            EmailHash = "hash",
            PasswordHash = "pw",
            FirstName = "New",
            LastName = "User",
            RoleId = 2,
            ReferredByUserId = 5,
            AppliedReferralCodeId = 99
        };

        var order = new Order
        {
            Id = 123,
            UserId = user.Id,
            OrderNumber = "ORD-REF-1",
            Status = OrderStatus.Paid,
            TotalAmount = 149.90m,
            ShippingAddress = "Test Address",
            Payment = new Payment
            {
                Id = 77,
                Amount = 149.90m,
                Status = PaymentStatus.Success,
                PaymentMethod = "CreditCard",
                IdempotencyKey = "payment-key"
            }
        };

        _orderDalMock.Setup(x => x.GetByIdWithDetailsAsync(order.Id))
            .ReturnsAsync(order);
        _userDalMock.Setup(x => x.GetAsync(It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>()))
            .ReturnsAsync(user);
        _orderDalMock.Setup(x => x.CountAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Order, bool>>>()))
            .ReturnsAsync(0);
        _referralTransactionDalMock.Setup(x => x.ExistsAsync(It.IsAny<System.Linq.Expressions.Expression<Func<ReferralTransaction, bool>>>()))
            .ReturnsAsync(false);
        _referralCodeDalMock.Setup(x => x.GetAsync(It.IsAny<System.Linq.Expressions.Expression<Func<ReferralCode, bool>>>()))
            .ReturnsAsync(new ReferralCode { Id = 99, UserId = 5, Code = "REFTEST01", IsActive = true });
        _loyaltyTransactionDalMock.Setup(x => x.GetAvailablePointsAsync(It.IsAny<int>(), It.IsAny<DateTime>()))
            .ReturnsAsync(100);

        var addedRewards = new List<ReferralTransaction>();
        _referralTransactionDalMock.Setup(x => x.AddRangeAsync(It.IsAny<IEnumerable<ReferralTransaction>>()))
            .Callback<IEnumerable<ReferralTransaction>>(items => addedRewards.AddRange(items))
            .Returns(Task.CompletedTask);

        var result = await _manager.AwardFirstPurchaseRewardsAsync(order.Id);

        result.Success.Should().BeTrue();
        user.ReferralRewardedOrderId.Should().Be(order.Id);
        addedRewards.Should().HaveCount(2);
        addedRewards.Should().Contain(x => x.Type == ReferralTransactionType.ReferrerRewardGranted && x.Points == 500);
        addedRewards.Should().Contain(x => x.Type == ReferralTransactionType.ReferredRewardGranted && x.Points == 250);
        _loyaltyTransactionDalMock.Verify(x => x.AddAsync(It.IsAny<LoyaltyTransaction>()), Times.Exactly(2));
    }
}
