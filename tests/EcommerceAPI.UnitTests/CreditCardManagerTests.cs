using EcommerceAPI.Business.Concrete;
using EcommerceAPI.Core.Interfaces;
using EcommerceAPI.DataAccess.Abstract;
using EcommerceAPI.Entities.Concrete;
using FluentAssertions;
using Moq;

namespace EcommerceAPI.UnitTests;

public class CreditCardManagerTests
{
    [Fact]
    public async Task GetStoredCardForPaymentAsync_WhenCardIsNotTokenized_ShouldRejectLegacyCardWithoutDecryptingPan()
    {
        var creditCardDalMock = new Mock<ICreditCardDal>();
        var unitOfWorkMock = new Mock<IUnitOfWork>();
        var encryptionServiceMock = new Mock<IEncryptionService>();
        var paymentFeaturePolicyMock = new Mock<EcommerceAPI.Business.Abstract.IPaymentFeaturePolicy>();
        paymentFeaturePolicyMock.SetupGet(x => x.AllowLegacyEncryptedSavedCardPayments).Returns(true);

        var manager = new CreditCardManager(
            creditCardDalMock.Object,
            unitOfWorkMock.Object,
            encryptionServiceMock.Object,
            paymentFeaturePolicyMock.Object);

        creditCardDalMock
            .Setup(x => x.GetAsync(It.IsAny<System.Linq.Expressions.Expression<Func<CreditCard, bool>>>()))
            .ReturnsAsync(new CreditCard
            {
                Id = 9,
                UserId = 42,
                CardAlias = "Eski Kart",
                CardHolderName = "Test User",
                CardNumberEncrypted = "enc-card",
                ExpireMonthEncrypted = "enc-month",
                ExpireYearEncrypted = "enc-year",
                Last4Digits = "4242"
            });

        var result = await manager.GetStoredCardForPaymentAsync(42, 9);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("yeniden kart bilgisi");
        encryptionServiceMock.Verify(x => x.Decrypt(It.IsAny<string>()), Times.Never);
    }
}
