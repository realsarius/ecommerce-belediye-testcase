using FluentAssertions;
using Moq;
using EcommerceAPI.Business.Abstract; 
using EcommerceAPI.Entities.Concrete;
using EcommerceAPI.Core.Interfaces;
using EcommerceAPI.Entities.DTOs;
using EcommerceAPI.Entities.Enums;
using EcommerceAPI.DataAccess.Abstract;
using Microsoft.Extensions.Options;
using EcommerceAPI.Infrastructure.Settings;
using EcommerceAPI.Infrastructure.ExternalServices;

namespace EcommerceAPI.UnitTests;

/// <summary>
/// IyzicoPaymentService (eski IyzicoPaymentManager) için unit testler.
/// 
/// Not: Bu sınıf Infrastructure katmanına taşındı.
/// IPaymentService interface'i üzerinden test edilir.
/// </summary>
public class IyzicoPaymentServiceTests
{
    private readonly Mock<IOrderDal> _orderDalMock;
    private readonly Mock<IPaymentDal> _paymentDalMock;
    private readonly Mock<IOptions<IyzicoSettings>> _optionsMock;
    private readonly Mock<IUnitOfWork> _uowMock;
    private readonly Mock<ICreditCardService> _creditCardServiceMock;
    private readonly IPaymentService _paymentService;

    public IyzicoPaymentServiceTests()
    {
        _orderDalMock = new Mock<IOrderDal>();
        _paymentDalMock = new Mock<IPaymentDal>();
        _uowMock = new Mock<IUnitOfWork>();
        _uowMock = new Mock<IUnitOfWork>();
        _creditCardServiceMock = new Mock<ICreditCardService>();
        _optionsMock = new Mock<IOptions<IyzicoSettings>>();
        
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
            new Mock<Microsoft.Extensions.Logging.ILogger<IyzicoPaymentService>>().Object
        );
    }

    [Fact]
    public async Task ProcessPaymentAsync_ShouldBeIdempotent_WhenCalledWithSameKey()
    {
        // Arrange
        var userId = 1;
        var orderId = 10;
        var idempotencyKey = "unique-key-123";
        
        var request = new ProcessPaymentRequest 
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

        // Act
        var result = await _paymentService.ProcessPaymentAsync(userId, request);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Status.Should().Be(PaymentStatus.Success.ToString());
        
        // Verify idempotency logic returns existing payment without processing new one
        _orderDalMock.Verify(x => x.Update(It.IsAny<Order>()), Times.Never);
        _uowMock.Verify(x => x.SaveChangesAsync(), Times.Never);
    }
}
