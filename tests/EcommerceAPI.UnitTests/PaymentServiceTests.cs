using FluentAssertions;
using Moq;
using EcommerceAPI.Business.Abstract; 
using EcommerceAPI.Entities.Concrete;
using EcommerceAPI.Core.Interfaces;
using EcommerceAPI.Entities.DTOs;
using EcommerceAPI.Entities.Enums;
using EcommerceAPI.DataAccess.Abstract;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using EcommerceAPI.Infrastructure.Settings;
using EcommerceAPI.Infrastructure.ExternalServices;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.Text;

namespace EcommerceAPI.UnitTests;

public class IyzicoPaymentServiceTests
{
    private readonly Mock<IOrderDal> _orderDalMock;
    private readonly Mock<IOptions<IyzicoSettings>> _optionsMock;
    private readonly Mock<IUnitOfWork> _uowMock;
    private readonly Mock<ICreditCardService> _creditCardServiceMock;
    private readonly Mock<IDistributedLockService> _lockServiceMock;
    private readonly Mock<ILogger<IyzicoPaymentService>> _loggerMock;
    private readonly IyzicoPaymentService _paymentService;

    public IyzicoPaymentServiceTests()
    {
        _orderDalMock = new Mock<IOrderDal>();
        _uowMock = new Mock<IUnitOfWork>();
        _optionsMock = new Mock<IOptions<IyzicoSettings>>();
        _creditCardServiceMock = new Mock<ICreditCardService>();
        _lockServiceMock = new Mock<IDistributedLockService>();
        _loggerMock = new Mock<ILogger<IyzicoPaymentService>>();
        
        _optionsMock.Setup(o => o.Value).Returns(new IyzicoSettings 
        { 
            ApiKey = "test", 
            SecretKey = "test", 
            BaseUrl = "https://sandbox-api.iyzipay.com" 
        });

        _lockServiceMock.Setup(x => x.TryAcquireLockAsync(It.IsAny<string>(), It.IsAny<int>()))
            .ReturnsAsync("lock-token");
        _lockServiceMock.Setup(x => x.ReleaseLockAsync(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        _paymentService = new IyzicoPaymentService(
            _orderDalMock.Object,
            _uowMock.Object,
            _optionsMock.Object,
            _creditCardServiceMock.Object,
            _lockServiceMock.Object,
            _loggerMock.Object
        );
    }

    [Fact]
    public async Task ProcessPaymentAsync_ShouldBeIdempotent_WhenCalledWithSameKey()
    {
        var userId = 1;
        var orderId = 10;
        var idempotencyKey = "unique-key-123";
        
        var paymentRequest = new ProcessPaymentRequest 
        { 
            OrderId = orderId, 
            CardHolderName = "Test", 
            CardNumber = "1234", 
            ExpiryDate = "12/30", 
            CVV = "123",
            IdempotencyKey = idempotencyKey
        };

        var existingOrder = new Order
        {
            Id = orderId,
            UserId = userId,
            Status = OrderStatus.Paid,
            TotalAmount = 100,
            Currency = "TRY",
            Payment = new Payment
            {
                Status = PaymentStatus.Success,
                IdempotencyKey = idempotencyKey,
                Amount = 100,
                Currency = "TRY",
                PaymentProviderId = "PAY-EXISTING"
            }
        };

        _orderDalMock.Setup(x => x.GetByIdWithDetailsAsync(orderId))
            .ReturnsAsync(existingOrder);

        var result = await _paymentService.ProcessPaymentAsync(userId, paymentRequest);

        result.Success.Should().BeTrue();
        result.Data.Status.Should().Be(PaymentStatus.Success.ToString());
        
        _orderDalMock.Verify(x => x.Update(It.IsAny<Order>()), Times.Never);
        _uowMock.Verify(x => x.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task ProcessPaymentAsync_WhenPaymentLockCannotBeAcquired_ShouldReturnSystemBusy()
    {
        _lockServiceMock.Setup(x => x.TryAcquireLockAsync(It.IsAny<string>(), It.IsAny<int>()))
            .ReturnsAsync((string?)null);

        var result = await _paymentService.ProcessPaymentAsync(1, new ProcessPaymentRequest
        {
            OrderId = 99,
            IdempotencyKey = "payment-key"
        });

        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be(EcommerceAPI.Infrastructure.Constants.InfrastructureConstants.Redis.SystemBusyCode);
        _orderDalMock.Verify(x => x.GetByIdWithDetailsAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task ProcessWebhookAsync_WhenOrderAlreadyPaid_ShouldReturnSuccessWithoutMutation()
    {
        var request = new IyzicoWebhookRequest
        {
            IyziEventType = "PAYMENT",
            PaymentId = "PAY-123",
            PaymentConversationId = "ORD-PAID",
            Status = "SUCCESS"
        };

        var existingOrder = new Order
        {
            Id = 77,
            OrderNumber = "ORD-PAID",
            Status = OrderStatus.Paid,
            Payment = new Payment
            {
                Status = PaymentStatus.Success,
                IdempotencyKey = "paid-key",
                Amount = 100,
                Currency = "TRY"
            }
        };

        _orderDalMock.Setup(x => x.GetByOrderNumberAsync("ORD-PAID"))
            .ReturnsAsync(existingOrder);

        var signature = ComputeSignature(request, "test");

        var result = await _paymentService.ProcessWebhookAsync(request, signature);

        result.Success.Should().BeTrue();
        result.Message.Should().Be("Already paid");
        _orderDalMock.Verify(x => x.Update(It.IsAny<Order>()), Times.Never);
        _uowMock.Verify(x => x.SaveChangesAsync(), Times.Never);
    }

    private static string ComputeSignature(IyzicoWebhookRequest request, string secretKey)
    {
        var payload = $"{secretKey}{request.IyziEventType}{request.PaymentId}{request.PaymentConversationId}{request.Status}";
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secretKey));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
