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

namespace EcommerceAPI.UnitTests;

public class IyzicoPaymentServiceTests
{
    private readonly Mock<IOrderDal> _orderDalMock;
    private readonly Mock<IOptions<IyzicoSettings>> _optionsMock;
    private readonly Mock<IUnitOfWork> _uowMock;
    private readonly Mock<ICreditCardService> _creditCardServiceMock;
    private readonly Mock<ILogger<IyzicoPaymentService>> _loggerMock;
    private readonly IyzicoPaymentService _paymentService;

    public IyzicoPaymentServiceTests()
    {
        _orderDalMock = new Mock<IOrderDal>();
        _uowMock = new Mock<IUnitOfWork>();
        _optionsMock = new Mock<IOptions<IyzicoSettings>>();
        _creditCardServiceMock = new Mock<ICreditCardService>();
        _loggerMock = new Mock<ILogger<IyzicoPaymentService>>();
        
        _optionsMock.Setup(o => o.Value).Returns(new IyzicoSettings 
        { 
            ApiKey = "test", 
            SecretKey = "test", 
            BaseUrl = "https://sandbox-api.iyzipay.com" 
        });

        _paymentService = new IyzicoPaymentService(
            _orderDalMock.Object,
            _uowMock.Object,
            _optionsMock.Object,
            _creditCardServiceMock.Object,
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
}

