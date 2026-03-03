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
using EcommerceAPI.Core.Utilities.Results;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.Text;

namespace EcommerceAPI.UnitTests;

public class IyzicoPaymentServiceTests
{
    private readonly Mock<IOrderDal> _orderDalMock;
    private readonly Mock<IOptions<IyzicoSettings>> _optionsMock;
    private readonly Mock<IOptions<PaymentSettings>> _paymentSettingsOptionsMock;
    private readonly Mock<IUnitOfWork> _uowMock;
    private readonly Mock<ICreditCardService> _creditCardServiceMock;
    private readonly Mock<ILoyaltyService> _loyaltyServiceMock;
    private readonly Mock<IReferralService> _referralServiceMock;
    private readonly Mock<IDistributedLockService> _lockServiceMock;
    private readonly Mock<ILogger<IyzicoPaymentService>> _loggerMock;
    private readonly IyzicoPaymentService _paymentService;

    public IyzicoPaymentServiceTests()
    {
        _orderDalMock = new Mock<IOrderDal>();
        _uowMock = new Mock<IUnitOfWork>();
        _optionsMock = new Mock<IOptions<IyzicoSettings>>();
        _paymentSettingsOptionsMock = new Mock<IOptions<PaymentSettings>>();
        _creditCardServiceMock = new Mock<ICreditCardService>();
        _loyaltyServiceMock = new Mock<ILoyaltyService>();
        _referralServiceMock = new Mock<IReferralService>();
        _lockServiceMock = new Mock<IDistributedLockService>();
        _loggerMock = new Mock<ILogger<IyzicoPaymentService>>();
        
        _optionsMock.Setup(o => o.Value).Returns(new IyzicoSettings 
        { 
            ApiKey = "test", 
            SecretKey = "test", 
            BaseUrl = "https://sandbox-api.iyzipay.com" 
        });
        _paymentSettingsOptionsMock.Setup(o => o.Value).Returns(new PaymentSettings
        {
            ActiveProviders = [PaymentProviderType.Iyzico],
            DefaultProvider = PaymentProviderType.Iyzico,
            Force3DSecure = false,
            Force3DSecureAbove = 5000m,
            PublicApiBaseUrl = "http://localhost:5294"
        });

        _lockServiceMock.Setup(x => x.TryAcquireLockAsync(It.IsAny<string>(), It.IsAny<int>()))
            .ReturnsAsync("lock-token");
        _lockServiceMock.Setup(x => x.ReleaseLockAsync(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);
        _loyaltyServiceMock
            .Setup(x => x.AwardPointsForOrderAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<decimal>()))
            .ReturnsAsync(new SuccessResult());
        _referralServiceMock
            .Setup(x => x.AwardFirstPurchaseRewardsAsync(It.IsAny<int>()))
            .ReturnsAsync(new SuccessResult());

        _paymentService = new IyzicoPaymentService(
            _orderDalMock.Object,
            _uowMock.Object,
            _optionsMock.Object,
            _paymentSettingsOptionsMock.Object,
            _creditCardServiceMock.Object,
            _loyaltyServiceMock.Object,
            _referralServiceMock.Object,
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
    public async Task ProcessPaymentAsync_WhenUnsupportedProviderRequested_ShouldReturnError()
    {
        var result = await _paymentService.ProcessPaymentAsync(1, new ProcessPaymentRequest
        {
            OrderId = 42,
            PaymentProvider = PaymentProviderType.Stripe,
            IdempotencyKey = "provider-check"
        });

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("henuz aktif degil");
        _lockServiceMock.Verify(x => x.TryAcquireLockAsync(It.IsAny<string>(), It.IsAny<int>()), Times.Never);
        _orderDalMock.Verify(x => x.GetByIdWithDetailsAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task ProcessPaymentAsync_WhenSavedCardBelongsToDifferentProvider_ShouldReturnReadableError()
    {
        var order = new Order
        {
            Id = 15,
            UserId = 1,
            OrderNumber = "ORD-15",
            Status = OrderStatus.PendingPayment,
            TotalAmount = 100,
            Currency = "TRY",
            ShippingAddress = "Test Address",
            Payment = new Payment
            {
                Status = PaymentStatus.Pending,
                Amount = 100,
                Currency = "TRY",
                PaymentMethod = "CreditCard"
            }
        };

        _orderDalMock.Setup(x => x.GetByIdWithDetailsAsync(order.Id))
            .ReturnsAsync(order);

        _creditCardServiceMock
            .Setup(x => x.GetStoredCardForPaymentAsync(order.UserId, 99))
            .ReturnsAsync(new SuccessDataResult<StoredCardPaymentDto>(new StoredCardPaymentDto
            {
                Id = 99,
                CardHolderName = "Test User",
                ExpireMonth = "12",
                ExpireYear = "2030",
                IsTokenized = true,
                TokenProvider = PaymentProviderType.Stripe
            }));

        var result = await _paymentService.ProcessPaymentAsync(order.UserId, new ProcessPaymentRequest
        {
            OrderId = order.Id,
            SavedCardId = 99,
            PaymentProvider = PaymentProviderType.Iyzico,
            IdempotencyKey = "saved-card-provider-check"
        });

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("yalnizca Iyzico ile alinabilir");
        _uowMock.Verify(x => x.SaveChangesAsync(), Times.Never);
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
