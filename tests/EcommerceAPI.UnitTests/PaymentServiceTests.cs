using FluentAssertions;
using Moq;
using EcommerceAPI.Business.Concrete;
using EcommerceAPI.Business.Abstract; 
using EcommerceAPI.Entities.Concrete;
using EcommerceAPI.Core.Interfaces;
using EcommerceAPI.Entities.DTOs;
using EcommerceAPI.Entities.Enums;
using EcommerceAPI.Core.Exceptions;
using EcommerceAPI.DataAccess.Abstract;
using Microsoft.Extensions.Options;
using EcommerceAPI.Business.Settings;
using System.Threading.Tasks;

namespace EcommerceAPI.UnitTests;

public class IyzicoPaymentManagerTests
{
    private readonly Mock<IOrderDal> _orderDalMock;
    private readonly Mock<IPaymentDal> _paymentDalMock;
    private readonly Mock<IOptions<IyzicoSettings>> _optionsMock;
    private readonly Mock<IUnitOfWork> _uowMock;
    private readonly IyzicoPaymentManager _paymentManager;

    public IyzicoPaymentManagerTests()
    {
        _orderDalMock = new Mock<IOrderDal>();
        _paymentDalMock = new Mock<IPaymentDal>();
        _uowMock = new Mock<IUnitOfWork>();
        _optionsMock = new Mock<IOptions<IyzicoSettings>>();
        
        _optionsMock.Setup(o => o.Value).Returns(new IyzicoSettings 
        { 
            ApiKey = "test", 
            SecretKey = "test", 
            BaseUrl = "https://sandbox-api.iyzipay.com" 
        });

        _paymentManager = new IyzicoPaymentManager(
            _orderDalMock.Object,
            _uowMock.Object,
            _optionsMock.Object
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

        var result = await _paymentManager.ProcessPaymentAsync(userId, paymentRequest);

        result.Success.Should().BeTrue();
        result.Data.Status.Should().Be(PaymentStatus.Success.ToString());
        
        _orderDalMock.Verify(x => x.Update(It.IsAny<Order>()), Times.Never);
        _uowMock.Verify(x => x.SaveChangesAsync(), Times.Never);
    }
}

