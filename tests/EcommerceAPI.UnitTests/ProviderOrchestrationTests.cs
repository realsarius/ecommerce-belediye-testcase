using EcommerceAPI.Business.Abstract;
using EcommerceAPI.Core.Utilities.Results;
using EcommerceAPI.DataAccess.Abstract;
using EcommerceAPI.Entities.Concrete;
using EcommerceAPI.Entities.DTOs;
using EcommerceAPI.Entities.Enums;
using EcommerceAPI.Infrastructure.Services;
using EcommerceAPI.Infrastructure.Settings;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;

namespace EcommerceAPI.UnitTests;

public class ProviderOrchestrationTests
{
    private static IOptions<PaymentSettings> CreatePaymentSettings(
        PaymentProviderType defaultProvider = PaymentProviderType.Iyzico,
        params PaymentProviderType[] activeProviders)
    {
        return Options.Create(new PaymentSettings
        {
            DefaultProvider = defaultProvider,
            ActiveProviders = activeProviders.Length == 0
                ? [PaymentProviderType.Iyzico]
                : activeProviders.ToList()
        });
    }

    [Fact]
    public void PaymentProviderFactory_GetProvider_ShouldReturnRegisteredProvider()
    {
        var paymentProvider = new Mock<IPaymentProvider>();
        paymentProvider.SetupGet(x => x.ProviderType).Returns(PaymentProviderType.Iyzico);

        var factory = new PaymentProviderFactory([paymentProvider.Object]);

        var provider = factory.GetProvider(PaymentProviderType.Iyzico);

        provider.Should().BeSameAs(paymentProvider.Object);
    }

    [Fact]
    public void PaymentProviderFactory_GetProvider_WhenProviderMissing_ShouldThrow()
    {
        var paymentProvider = new Mock<IPaymentProvider>();
        paymentProvider.SetupGet(x => x.ProviderType).Returns(PaymentProviderType.Iyzico);

        var factory = new PaymentProviderFactory([paymentProvider.Object]);

        var act = () => factory.GetProvider(PaymentProviderType.Stripe);

        act.Should().Throw<NotSupportedException>()
            .WithMessage("*Stripe*");
    }

    [Fact]
    public void RefundProviderFactory_GetProvider_ShouldReturnRegisteredProvider()
    {
        var refundProvider = new Mock<IRefundProvider>();
        refundProvider.SetupGet(x => x.ProviderType).Returns(PaymentProviderType.Iyzico);

        var factory = new RefundProviderFactory([refundProvider.Object]);

        var provider = factory.GetProvider(PaymentProviderType.Iyzico);

        provider.Should().BeSameAs(refundProvider.Object);
    }

    [Fact]
    public void RefundProviderFactory_GetProvider_WhenProviderMissing_ShouldThrow()
    {
        var refundProvider = new Mock<IRefundProvider>();
        refundProvider.SetupGet(x => x.ProviderType).Returns(PaymentProviderType.Iyzico);

        var factory = new RefundProviderFactory([refundProvider.Object]);

        var act = () => factory.GetProvider(PaymentProviderType.Stripe);

        act.Should().Throw<NotSupportedException>()
            .WithMessage("*Stripe*");
    }

    [Fact]
    public async Task PaymentService_ProcessPaymentAsync_ShouldDelegateToRequestedProvider()
    {
        var providerFactoryMock = new Mock<IPaymentProviderFactory>();
        var orderDalMock = new Mock<IOrderDal>();
        var providerMock = new Mock<IPaymentProvider>();

        providerMock.SetupGet(x => x.ProviderType).Returns(PaymentProviderType.Iyzico);
        providerMock
            .Setup(x => x.ProcessPaymentAsync(7, It.IsAny<ProcessPaymentRequest>()))
            .ReturnsAsync(new SuccessDataResult<PaymentDto>(new PaymentDto
            {
                Id = 1,
                Status = PaymentStatus.Success.ToString(),
                Provider = PaymentProviderType.Iyzico
            }));

        providerFactoryMock
            .Setup(x => x.GetProvider(PaymentProviderType.Iyzico))
            .Returns(providerMock.Object);

        var service = new PaymentService(
            providerFactoryMock.Object,
            orderDalMock.Object,
            CreatePaymentSettings(PaymentProviderType.Iyzico, PaymentProviderType.Iyzico));

        var result = await service.ProcessPaymentAsync(7, new ProcessPaymentRequest
        {
            OrderId = 12,
            PaymentProvider = PaymentProviderType.Iyzico
        });

        result.Success.Should().BeTrue();
        result.Data.Provider.Should().Be(PaymentProviderType.Iyzico);
        providerFactoryMock.Verify(x => x.GetProvider(PaymentProviderType.Iyzico), Times.Once);
        providerMock.Verify(x => x.ProcessPaymentAsync(7, It.Is<ProcessPaymentRequest>(request =>
            request.OrderId == 12 &&
            request.PaymentProvider == PaymentProviderType.Iyzico)), Times.Once);
    }

    [Fact]
    public async Task PaymentService_ProcessPaymentAsync_ShouldForwardRequireThreeDSFlag()
    {
        var providerFactoryMock = new Mock<IPaymentProviderFactory>();
        var orderDalMock = new Mock<IOrderDal>();
        var providerMock = new Mock<IPaymentProvider>();

        providerMock.SetupGet(x => x.ProviderType).Returns(PaymentProviderType.Iyzico);
        providerMock
            .Setup(x => x.ProcessPaymentAsync(7, It.IsAny<ProcessPaymentRequest>()))
            .ReturnsAsync(new SuccessDataResult<PaymentDto>(new PaymentDto
            {
                Id = 2,
                Status = PaymentStatus.Pending.ToString(),
                Provider = PaymentProviderType.Iyzico,
                RequiresThreeDS = true
            }));

        providerFactoryMock
            .Setup(x => x.GetProvider(PaymentProviderType.Iyzico))
            .Returns(providerMock.Object);

        var service = new PaymentService(
            providerFactoryMock.Object,
            orderDalMock.Object,
            CreatePaymentSettings(PaymentProviderType.Iyzico, PaymentProviderType.Iyzico));

        var result = await service.ProcessPaymentAsync(7, new ProcessPaymentRequest
        {
            OrderId = 15,
            PaymentProvider = PaymentProviderType.Iyzico,
            Require3DS = true
        });

        result.Success.Should().BeTrue();
        result.Data.RequiresThreeDS.Should().BeTrue();
        providerMock.Verify(x => x.ProcessPaymentAsync(7, It.Is<ProcessPaymentRequest>(request =>
            request.OrderId == 15 &&
            request.Require3DS)), Times.Once);
    }

    [Fact]
    public async Task PaymentService_GetPaymentByOrderIdAsync_ShouldUseStoredPaymentProvider()
    {
        var providerFactoryMock = new Mock<IPaymentProviderFactory>();
        var orderDalMock = new Mock<IOrderDal>();
        var providerMock = new Mock<IPaymentProvider>();

        orderDalMock.Setup(x => x.GetByIdWithDetailsAsync(25))
            .ReturnsAsync(new Order
            {
                Id = 25,
                UserId = 3,
                Status = OrderStatus.Paid,
                TotalAmount = 99,
                ShippingAddress = "Test Address",
                Payment = new Payment
                {
                    Id = 5,
                    OrderId = 25,
                    Provider = PaymentProviderType.Iyzico,
                    Status = PaymentStatus.Success,
                    Amount = 99,
                    Currency = "TRY",
                    PaymentMethod = "CreditCard"
                }
            });

        providerMock
            .Setup(x => x.GetPaymentByOrderIdAsync(25))
            .ReturnsAsync(new SuccessDataResult<PaymentDto>(new PaymentDto
            {
                Id = 5,
                Provider = PaymentProviderType.Iyzico,
                Status = PaymentStatus.Success.ToString()
            }));

        providerFactoryMock
            .Setup(x => x.GetProvider(PaymentProviderType.Iyzico))
            .Returns(providerMock.Object);

        var service = new PaymentService(
            providerFactoryMock.Object,
            orderDalMock.Object,
            CreatePaymentSettings(PaymentProviderType.Iyzico, PaymentProviderType.Iyzico));

        var result = await service.GetPaymentByOrderIdAsync(25);

        result.Success.Should().BeTrue();
        result.Data.Provider.Should().Be(PaymentProviderType.Iyzico);
        providerFactoryMock.Verify(x => x.GetProvider(PaymentProviderType.Iyzico), Times.Once);
        providerMock.Verify(x => x.GetPaymentByOrderIdAsync(25), Times.Once);
    }

    [Fact]
    public async Task PaymentService_ProcessPaymentAsync_ShouldRejectInactiveProvider()
    {
        var providerFactoryMock = new Mock<IPaymentProviderFactory>();
        var orderDalMock = new Mock<IOrderDal>();

        var service = new PaymentService(
            providerFactoryMock.Object,
            orderDalMock.Object,
            CreatePaymentSettings(PaymentProviderType.Iyzico, PaymentProviderType.Iyzico));

        var result = await service.ProcessPaymentAsync(7, new ProcessPaymentRequest
        {
            OrderId = 12,
            PaymentProvider = PaymentProviderType.Stripe
        });

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("aktif degil");
        providerFactoryMock.Verify(x => x.GetProvider(It.IsAny<PaymentProviderType>()), Times.Never);
    }

    [Fact]
    public async Task RefundService_ProcessRefundAsync_ShouldDelegateToStoredPaymentProvider()
    {
        var refundProviderFactoryMock = new Mock<IRefundProviderFactory>();
        var refundRequestDalMock = new Mock<IRefundRequestDal>();
        var refundProviderMock = new Mock<IRefundProvider>();

        refundRequestDalMock.Setup(x => x.GetByIdWithDetailsAsync(44))
            .ReturnsAsync(new RefundRequest
            {
                Id = 44,
                OrderId = 77,
                Payment = new Payment
                {
                    Id = 11,
                    Provider = PaymentProviderType.Iyzico,
                    Status = PaymentStatus.Success,
                    PaymentMethod = "CreditCard",
                    Amount = 120,
                    Currency = "TRY"
                },
                ReturnRequest = new ReturnRequest
                {
                    Id = 9,
                    UserId = 2,
                    Status = ReturnRequestStatus.RefundPending,
                    Reason = "Test",
                    Type = ReturnRequestType.Return
                },
                Order = new Order
                {
                    Id = 77,
                    UserId = 2,
                    OrderNumber = "ORD-77",
                    Status = OrderStatus.Paid,
                    TotalAmount = 120,
                    ShippingAddress = "Test Address"
                }
            });

        refundProviderMock
            .Setup(x => x.ProcessRefundAsync(44, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SuccessDataResult<RefundRequestDto>(new RefundRequestDto
            {
                Id = 44,
                OrderId = 77,
                Status = RefundRequestStatus.Succeeded.ToString()
            }));

        refundProviderFactoryMock
            .Setup(x => x.GetProvider(PaymentProviderType.Iyzico))
            .Returns(refundProviderMock.Object);

        var service = new RefundService(refundProviderFactoryMock.Object, refundRequestDalMock.Object);

        var result = await service.ProcessRefundAsync(44);

        result.Success.Should().BeTrue();
        result.Data.Status.Should().Be(RefundRequestStatus.Succeeded.ToString());
        refundProviderFactoryMock.Verify(x => x.GetProvider(PaymentProviderType.Iyzico), Times.Once);
        refundProviderMock.Verify(x => x.ProcessRefundAsync(44, It.IsAny<CancellationToken>()), Times.Once);
    }
}
