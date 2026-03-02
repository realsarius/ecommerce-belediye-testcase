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

public class GiftCardManagerTests
{
    private readonly Mock<IGiftCardDal> _giftCardDalMock;
    private readonly Mock<IGiftCardTransactionDal> _giftCardTransactionDalMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IAuditService> _auditServiceMock;
    private readonly GiftCardManager _manager;

    public GiftCardManagerTests()
    {
        _giftCardDalMock = new Mock<IGiftCardDal>();
        _giftCardTransactionDalMock = new Mock<IGiftCardTransactionDal>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _auditServiceMock = new Mock<IAuditService>();

        _manager = new GiftCardManager(
            _giftCardDalMock.Object,
            _giftCardTransactionDalMock.Object,
            _unitOfWorkMock.Object,
            _auditServiceMock.Object);
    }

    [Fact]
    public async Task ValidateAsync_ShouldApplyAvailableBalance()
    {
        _giftCardDalMock.Setup(x => x.GetByCodeAsync("GC-VALID"))
            .ReturnsAsync(new GiftCard
            {
                Id = 1,
                Code = "GC-VALID",
                InitialBalance = 200m,
                CurrentBalance = 150m,
                IsActive = true
            });

        var result = await _manager.ValidateAsync(42, "gc-valid", 90m);

        result.Success.Should().BeTrue();
        result.Data.IsValid.Should().BeTrue();
        result.Data.AppliedAmount.Should().Be(90m);
        result.Data.RemainingBalance.Should().Be(60m);
    }

    [Fact]
    public async Task RedeemForOrderAsync_ShouldAssignGiftCardAndCreateNegativeTransaction()
    {
        var giftCard = new GiftCard
        {
            Id = 3,
            Code = "GC-REDEEM",
            InitialBalance = 250m,
            CurrentBalance = 250m,
            IsActive = true
        };

        _giftCardTransactionDalMock.Setup(x => x.GetByOrderAndTypeAsync(1001, GiftCardTransactionType.Redeemed))
            .ReturnsAsync((GiftCardTransaction?)null);
        _giftCardDalMock.Setup(x => x.GetByCodeAsync("GC-REDEEM"))
            .ReturnsAsync(giftCard);

        GiftCardTransaction? captured = null;
        _giftCardTransactionDalMock.Setup(x => x.AddAsync(It.IsAny<GiftCardTransaction>()))
            .Callback<GiftCardTransaction>(tx => captured = tx)
            .ReturnsAsync((GiftCardTransaction tx) => tx);

        var result = await _manager.RedeemForOrderAsync(42, 1001, "GC-REDEEM", 75m, "checkout");

        result.Success.Should().BeTrue();
        giftCard.AssignedUserId.Should().Be(42);
        giftCard.CurrentBalance.Should().Be(175m);
        captured.Should().NotBeNull();
        captured!.Amount.Should().Be(-75m);
        captured.BalanceAfter.Should().Be(175m);
    }

    [Fact]
    public async Task RestoreForOrderAsync_ShouldCreatePositiveTransaction()
    {
        var giftCard = new GiftCard
        {
            Id = 3,
            Code = "GC-RESTORE",
            InitialBalance = 250m,
            CurrentBalance = 125m,
            IsActive = true,
            AssignedUserId = 42
        };

        _giftCardTransactionDalMock.Setup(x => x.GetByOrderAndTypeAsync(1001, GiftCardTransactionType.Redeemed))
            .ReturnsAsync(new GiftCardTransaction
            {
                Id = 90,
                GiftCardId = giftCard.Id,
                GiftCard = giftCard,
                OrderId = 1001,
                Type = GiftCardTransactionType.Redeemed,
                Amount = -75m,
                BalanceAfter = 125m,
                Description = "checkout"
            });
        _giftCardTransactionDalMock.Setup(x => x.GetByOrderAndTypeAsync(1001, GiftCardTransactionType.Restored))
            .ReturnsAsync((GiftCardTransaction?)null);

        GiftCardTransaction? captured = null;
        _giftCardTransactionDalMock.Setup(x => x.AddAsync(It.IsAny<GiftCardTransaction>()))
            .Callback<GiftCardTransaction>(tx => captured = tx)
            .ReturnsAsync((GiftCardTransaction tx) => tx);

        var result = await _manager.RestoreForOrderAsync(42, 1001, "cancel");

        result.Success.Should().BeTrue();
        giftCard.CurrentBalance.Should().Be(200m);
        captured.Should().NotBeNull();
        captured!.Amount.Should().Be(75m);
        captured.BalanceAfter.Should().Be(200m);
    }
}
