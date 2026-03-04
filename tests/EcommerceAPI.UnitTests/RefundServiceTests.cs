using EcommerceAPI.Business.Abstract;
using EcommerceAPI.Core.CrossCuttingConcerns.Logging;
using EcommerceAPI.Core.Interfaces;
using EcommerceAPI.DataAccess.Abstract;
using EcommerceAPI.Entities.Concrete;
using EcommerceAPI.Entities.Enums;
using EcommerceAPI.Infrastructure.ExternalServices;
using EcommerceAPI.Infrastructure.Utilities;
using EcommerceAPI.Core.Utilities.Results;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using EcommerceAPI.Core.CrossCuttingConcerns;

namespace EcommerceAPI.UnitTests;

public class RefundServiceTests
{
    private readonly Mock<IRefundRequestDal> _refundRequestDalMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IIyzicoRefundGateway> _refundGatewayMock;
    private readonly Mock<ILoyaltyService> _loyaltyServiceMock;
    private readonly Mock<IGiftCardService> _giftCardServiceMock;
    private readonly Mock<IReferralService> _referralServiceMock;
    private readonly Mock<IAuditService> _auditServiceMock;
    private readonly Mock<ILogger<IyzicoRefundService>> _loggerMock;
    private readonly Mock<ICorrelationIdProvider> _correlationIdProviderMock;
    private readonly IRefundService _refundService;

    public RefundServiceTests()
    {
        _refundRequestDalMock = new Mock<IRefundRequestDal>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _refundGatewayMock = new Mock<IIyzicoRefundGateway>();
        _loyaltyServiceMock = new Mock<ILoyaltyService>();
        _giftCardServiceMock = new Mock<IGiftCardService>();
        _referralServiceMock = new Mock<IReferralService>();
        _auditServiceMock = new Mock<IAuditService>();
        _loggerMock = new Mock<ILogger<IyzicoRefundService>>();
        _correlationIdProviderMock = new Mock<ICorrelationIdProvider>();
        _correlationIdProviderMock.Setup(x => x.GetCorrelationId()).Returns("test-correlation-id");
        _loyaltyServiceMock.Setup(x => x.RestoreRedeemedPointsAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>()))
            .ReturnsAsync(new SuccessResult());
        _loyaltyServiceMock.Setup(x => x.ReverseEarnedPointsAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>()))
            .ReturnsAsync(new SuccessResult());
        _giftCardServiceMock.Setup(x => x.RestoreForOrderAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>()))
            .ReturnsAsync(new SuccessResult());
        _referralServiceMock.Setup(x => x.ReverseRewardsForOrderAsync(It.IsAny<int>(), It.IsAny<string>()))
            .ReturnsAsync(new SuccessResult());

        _refundService = new IyzicoRefundService(
            _refundRequestDalMock.Object,
            _unitOfWorkMock.Object,
            _refundGatewayMock.Object,
            _loyaltyServiceMock.Object,
            _giftCardServiceMock.Object,
            _referralServiceMock.Object,
            _auditServiceMock.Object,
            _loggerMock.Object,
            _correlationIdProviderMock.Object);
    }

    [Fact]
    public async Task ProcessRefundAsync_WhenGatewaySucceeds_ShouldUpdateStatuses()
    {
        var refundRequest = CreateRefundRequest();

        _refundRequestDalMock.Setup(x => x.GetByIdWithDetailsAsync(refundRequest.Id))
            .ReturnsAsync(refundRequest);
        _refundGatewayMock.Setup(x => x.RefundAsync(It.IsAny<IyzicoRefundGatewayRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new IyzicoRefundGatewayResult(true, "HOST-REF-1", null, null));

        var result = await _refundService.ProcessRefundAsync(refundRequest.Id);

        result.Success.Should().BeTrue();
        result.Data.Status.Should().Be(RefundRequestStatus.Succeeded.ToString());
        refundRequest.Status.Should().Be(RefundRequestStatus.Succeeded);
        refundRequest.ReturnRequest.Status.Should().Be(ReturnRequestStatus.Refunded);
        refundRequest.Order.Status.Should().Be(OrderStatus.Refunded);
        refundRequest.Payment!.Status.Should().Be(PaymentStatus.Refunded);
        _giftCardServiceMock.Verify(x => x.RestoreForOrderAsync(refundRequest.ReturnRequest.UserId, refundRequest.OrderId, It.IsAny<string>()), Times.Once);
        _referralServiceMock.Verify(x => x.ReverseRewardsForOrderAsync(refundRequest.OrderId, It.IsAny<string>()), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(), Times.Exactly(2));
    }

    [Fact]
    public async Task ProcessRefundAsync_WhenGatewayReturnsFailure_ShouldMarkRefundFailed()
    {
        var refundRequest = CreateRefundRequest();

        _refundRequestDalMock.Setup(x => x.GetByIdWithDetailsAsync(refundRequest.Id))
            .ReturnsAsync(refundRequest);
        _refundGatewayMock.Setup(x => x.RefundAsync(It.IsAny<IyzicoRefundGatewayRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new IyzicoRefundGatewayResult(false, null, "Gateway rejection", "GATEWAY_ERR"));

        var result = await _refundService.ProcessRefundAsync(refundRequest.Id);

        result.Success.Should().BeFalse();
        refundRequest.Status.Should().Be(RefundRequestStatus.Failed);
        refundRequest.ReturnRequest.Status.Should().Be(ReturnRequestStatus.RefundPending);
        refundRequest.Order.Status.Should().NotBe(OrderStatus.Refunded);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(), Times.Exactly(2));
    }

    [Fact]
    public async Task ProcessRefundAsync_WhenGatewayReturnsSensitiveFailure_ShouldSanitizeLogs()
    {
        var refundRequest = CreateRefundRequest();
        const string rawMessage = "cardNumber=4111111111111111 cardToken=tok_test cvv=123";

        _refundRequestDalMock.Setup(x => x.GetByIdWithDetailsAsync(refundRequest.Id))
            .ReturnsAsync(refundRequest);
        _refundGatewayMock.Setup(x => x.RefundAsync(It.IsAny<IyzicoRefundGatewayRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new IyzicoRefundGatewayResult(false, null, rawMessage, "GATEWAY_ERR"));

        await _refundService.ProcessRefundAsync(refundRequest.Id);

        _loggerMock.VerifyLogContains(LogLevel.Warning, "[REDACTED]");
        _loggerMock.VerifyLogDoesNotContain("4111111111111111");
        _loggerMock.VerifyLogDoesNotContain("tok_test");
        _loggerMock.VerifyLogDoesNotContain("cvv=123");
    }

    [Fact]
    public async Task ProcessRefundAsync_WhenGatewayThrowsSensitiveException_ShouldSanitizeLogs()
    {
        var refundRequest = CreateRefundRequest();
        var exception = new InvalidOperationException("pan=4111111111111111 token=tok_secret cvv 123");

        _refundRequestDalMock.Setup(x => x.GetByIdWithDetailsAsync(refundRequest.Id))
            .ReturnsAsync(refundRequest);
        _refundGatewayMock.Setup(x => x.RefundAsync(It.IsAny<IyzicoRefundGatewayRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(exception);

        var act = () => _refundService.ProcessRefundAsync(refundRequest.Id);

        await act.Should().ThrowAsync<InvalidOperationException>();

        _loggerMock.VerifyLogContains(LogLevel.Error, "[REDACTED]");
        _loggerMock.VerifyLogDoesNotContain("4111111111111111");
        _loggerMock.VerifyLogDoesNotContain("tok_secret");
        _loggerMock.VerifyLogDoesNotContain("cvv 123");
    }

    [Fact]
    public void SensitiveDataLogSanitizer_ShouldMaskCardAndTokenValues()
    {
        const string rawValue = "cardNumber=4111111111111111 cardToken=tok_test cvv=123 security code 456";

        var sanitized = SensitiveDataLogSanitizer.Sanitize(rawValue);

        sanitized.Should().NotBeNull();
        sanitized.Should().Contain("[REDACTED]");
        sanitized.Should().NotContain("4111111111111111");
        sanitized.Should().NotContain("tok_test");
        sanitized.Should().NotContain("123");
        sanitized.Should().NotContain("456");
    }

    private static RefundRequest CreateRefundRequest()
    {
        var user = new User
        {
            Id = 42,
            FirstName = "Test",
            LastName = "Customer",
            Email = "customer@test.com",
            EmailHash = "hash",
            PasswordHash = "pw",
            RoleId = 1
        };

        var order = new Order
        {
            Id = 501,
            UserId = user.Id,
            OrderNumber = "ORD-REFUND-1",
            Status = OrderStatus.Paid,
            TotalAmount = 249.90m,
            GiftCardAmount = 50m,
            Currency = "TRY",
            ShippingAddress = "Test Address"
        };

        var payment = new Payment
        {
            Id = 801,
            OrderId = order.Id,
            Amount = 249.90m,
            Currency = "TRY",
            PaymentMethod = "CreditCard",
            Provider = PaymentProviderType.Iyzico,
            PaymentProviderId = "PAYMENT-1",
            Status = PaymentStatus.Success,
            IdempotencyKey = "payment-key"
        };

        order.Payment = payment;

        var returnRequest = new ReturnRequest
        {
            Id = 3001,
            OrderId = order.Id,
            UserId = user.Id,
            Status = ReturnRequestStatus.RefundPending,
            Type = ReturnRequestType.Return,
            Reason = "İade",
            RequestedRefundAmount = 249.90m,
            Order = order,
            User = user
        };

        return new RefundRequest
        {
            Id = 4001,
            ReturnRequestId = returnRequest.Id,
            OrderId = order.Id,
            PaymentId = payment.Id,
            Provider = PaymentProviderType.Iyzico,
            Amount = 249.90m,
            Status = RefundRequestStatus.Pending,
            IdempotencyKey = "refund-key",
            ReturnRequest = returnRequest,
            Order = order,
            Payment = payment
        };
    }
}

internal static class LoggerMockExtensions
{
    public static void VerifyLogContains<T>(this Mock<ILogger<T>> loggerMock, LogLevel level, string expectedValue)
    {
        loggerMock.Verify(
            logger => logger.Log(
                level,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((state, _) => state.ToString()!.Contains(expectedValue, StringComparison.Ordinal)),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    public static void VerifyLogDoesNotContain<T>(this Mock<ILogger<T>> loggerMock, string unexpectedValue)
    {
        loggerMock.Verify(
            logger => logger.Log(
                It.IsAny<LogLevel>(),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((state, _) => state.ToString()!.Contains(unexpectedValue, StringComparison.Ordinal)),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Never);
    }
}
