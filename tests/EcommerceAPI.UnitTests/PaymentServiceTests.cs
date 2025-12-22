using FluentAssertions;
using Moq;
using EcommerceAPI.Business.Services.Concrete;
using EcommerceAPI.Business.Services.Abstract; 
using EcommerceAPI.Core.Entities;
using EcommerceAPI.Core.Interfaces;
using EcommerceAPI.Core.DTOs;
using EcommerceAPI.Core.Enums;
using EcommerceAPI.Core.Exceptions;

namespace EcommerceAPI.UnitTests;

public class PaymentServiceTests
{
    private readonly Mock<IOrderRepository> _orderRepo;
    private readonly Mock<IUnitOfWork> _uow;
    private readonly PaymentService _paymentService;

    public PaymentServiceTests()
    {
        _orderRepo = new Mock<IOrderRepository>();
        _uow = new Mock<IUnitOfWork>();
        _paymentService = new PaymentService(_orderRepo.Object, _uow.Object);
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
            Payment = new Payment
            {
                Status = PaymentStatus.Success,
                IdempotencyKey = idempotencyKey,
                Amount = 100,
                PaymentProviderId = "PAY-EXISTING"
            }
        };

        _orderRepo.Setup(x => x.GetByIdWithDetailsAsync(orderId))
            .ReturnsAsync(existingOrder);

        // Act
        var result = await _paymentService.ProcessPaymentAsync(userId, request);

        // Assert
        result.Status.Should().Be(PaymentStatus.Success.ToString());
        
        
        // Verify we implied it's the existing one: Logic returns early.
        // We can verify that Update/Save was NOT called for idempotency return?
        // Code: if (idempotency match) return MapToDto.
        // So Update/Save shouldn't be called.
        _orderRepo.Verify(x => x.Update(It.IsAny<Order>()), Times.Never);
        _uow.Verify(x => x.SaveChangesAsync(), Times.Never);
    }
}
