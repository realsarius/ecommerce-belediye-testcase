using EcommerceAPI.Business.Abstract;
using EcommerceAPI.Core.CrossCuttingConcerns.Logging;
using EcommerceAPI.Core.Interfaces;
using EcommerceAPI.DataAccess.Abstract;
using EcommerceAPI.Entities.Concrete;
using EcommerceAPI.Entities.Enums;
using EcommerceAPI.Infrastructure.ExternalServices;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace EcommerceAPI.UnitTests;

public class RefundServiceTests
{
    private readonly Mock<IRefundRequestDal> _refundRequestDalMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IIyzicoRefundGateway> _refundGatewayMock;
    private readonly Mock<IAuditService> _auditServiceMock;
    private readonly Mock<ILogger<IyzicoRefundService>> _loggerMock;
    private readonly IRefundService _refundService;

    public RefundServiceTests()
    {
        _refundRequestDalMock = new Mock<IRefundRequestDal>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _refundGatewayMock = new Mock<IIyzicoRefundGateway>();
        _auditServiceMock = new Mock<IAuditService>();
        _loggerMock = new Mock<ILogger<IyzicoRefundService>>();

        _refundService = new IyzicoRefundService(
            _refundRequestDalMock.Object,
            _unitOfWorkMock.Object,
            _refundGatewayMock.Object,
            _auditServiceMock.Object,
            _loggerMock.Object);
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
            Amount = 249.90m,
            Status = RefundRequestStatus.Pending,
            IdempotencyKey = "refund-key",
            ReturnRequest = returnRequest,
            Order = order,
            Payment = payment
        };
    }
}
