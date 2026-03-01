using EcommerceAPI.Business.Concrete;
using EcommerceAPI.Core.CrossCuttingConcerns.Logging;
using EcommerceAPI.Core.Interfaces;
using EcommerceAPI.Core.Utilities.Results;
using EcommerceAPI.DataAccess.Abstract;
using EcommerceAPI.Entities.Concrete;
using EcommerceAPI.Entities.DTOs;
using EcommerceAPI.Entities.Enums;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace EcommerceAPI.UnitTests;

public class ReturnRequestManagerTests
{
    private readonly Mock<IReturnRequestDal> _returnRequestDalMock;
    private readonly Mock<IRefundRequestDal> _refundRequestDalMock;
    private readonly Mock<IOrderDal> _orderDalMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IAuditService> _auditServiceMock;
    private readonly Mock<ILogger<ReturnRequestManager>> _loggerMock;
    private readonly ReturnRequestManager _manager;

    public ReturnRequestManagerTests()
    {
        _returnRequestDalMock = new Mock<IReturnRequestDal>();
        _refundRequestDalMock = new Mock<IRefundRequestDal>();
        _orderDalMock = new Mock<IOrderDal>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _auditServiceMock = new Mock<IAuditService>();
        _loggerMock = new Mock<ILogger<ReturnRequestManager>>();

        _manager = new ReturnRequestManager(
            _returnRequestDalMock.Object,
            _refundRequestDalMock.Object,
            _orderDalMock.Object,
            _unitOfWorkMock.Object,
            _auditServiceMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task CreateReturnRequestAsync_ForDeliveredOrder_ShouldCreatePendingReturnRequest()
    {
        var order = CreateOrder(OrderStatus.Delivered, PaymentStatus.Success);

        _orderDalMock.Setup(x => x.GetByIdWithDetailsAsync(order.Id))
            .ReturnsAsync(order);
        _returnRequestDalMock.Setup(x => x.HasActiveRequestForOrderAsync(order.Id))
            .ReturnsAsync(false);
        _returnRequestDalMock.Setup(x => x.AddAsync(It.IsAny<ReturnRequest>()))
            .Callback<ReturnRequest>(request => request.Id = 1001)
            .ReturnsAsync((ReturnRequest request) => request);
        _returnRequestDalMock.Setup(x => x.GetByIdWithDetailsAsync(1001))
            .ReturnsAsync((ReturnRequest?)null);

        var result = await _manager.CreateReturnRequestAsync(order.UserId, order.Id, new CreateReturnRequestRequest
        {
            Type = "Return",
            Reason = "Ürün beklentimi karşılamadı",
            RequestNote = "Kutusu açıldı ama hasarsız."
        });

        result.Success.Should().BeTrue();
        result.Data.Status.Should().Be(ReturnRequestStatus.Pending.ToString());
        result.Data.Type.Should().Be(ReturnRequestType.Return.ToString());
        result.Data.RequestedRefundAmount.Should().Be(order.TotalAmount);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task CreateReturnRequestAsync_ForUndeliveredReturn_ShouldFail()
    {
        var order = CreateOrder(OrderStatus.Paid, PaymentStatus.Success);

        _orderDalMock.Setup(x => x.GetByIdWithDetailsAsync(order.Id))
            .ReturnsAsync(order);
        _returnRequestDalMock.Setup(x => x.HasActiveRequestForOrderAsync(order.Id))
            .ReturnsAsync(false);

        var result = await _manager.CreateReturnRequestAsync(order.UserId, order.Id, new CreateReturnRequestRequest
        {
            Type = "Return",
            Reason = "İade denemesi"
        });

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("teslim edilen");
        _returnRequestDalMock.Verify(x => x.AddAsync(It.IsAny<ReturnRequest>()), Times.Never);
    }

    [Fact]
    public async Task ReviewReturnRequestAsync_ApprovedPaidOrder_ShouldCreateRefundRequest()
    {
        var returnRequest = new ReturnRequest
        {
            Id = 3001,
            OrderId = 501,
            UserId = 42,
            Type = ReturnRequestType.Return,
            Status = ReturnRequestStatus.Pending,
            Reason = "İade",
            RequestedRefundAmount = 249.90m,
            Order = CreateOrder(OrderStatus.Delivered, PaymentStatus.Success),
            User = new User { Id = 42, FirstName = "Test", LastName = "Customer", Email = "customer@test.com", EmailHash = "hash", PasswordHash = "pw", RoleId = 1 }
        };
        returnRequest.Order.Id = 501;

        _returnRequestDalMock.Setup(x => x.GetByIdWithDetailsAsync(returnRequest.Id))
            .ReturnsAsync(returnRequest);
        _refundRequestDalMock.Setup(x => x.GetByReturnRequestIdAsync(returnRequest.Id))
            .ReturnsAsync((RefundRequest?)null);
        _refundRequestDalMock.Setup(x => x.AddAsync(It.IsAny<RefundRequest>()))
            .ReturnsAsync((RefundRequest refundRequest) => refundRequest);

        var result = await _manager.ReviewReturnRequestAsync(returnRequest.Id, 7, new ReviewReturnRequestRequest
        {
            Status = "Approved",
            ReviewNote = "Onaylandı"
        });

        result.Success.Should().BeTrue();
        result.Data.Status.Should().Be(ReturnRequestStatus.RefundPending.ToString());
        _refundRequestDalMock.Verify(x => x.AddAsync(It.IsAny<RefundRequest>()), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(), Times.Once);
    }

    private static Order CreateOrder(OrderStatus orderStatus, PaymentStatus paymentStatus)
    {
        return new Order
        {
            Id = 501,
            UserId = 42,
            OrderNumber = "ORD-TEST",
            Status = orderStatus,
            TotalAmount = 249.90m,
            ShippingAddress = "Test Address",
            Payment = new Payment
            {
                Id = 801,
                Amount = 249.90m,
                Currency = "TRY",
                Status = paymentStatus,
                PaymentMethod = "CreditCard",
                IdempotencyKey = "payment-key"
            },
            OrderItems =
            [
                new OrderItem
                {
                    ProductId = 91,
                    Quantity = 1,
                    PriceSnapshot = 249.90m,
                    Product = new Product
                    {
                        Id = 91,
                        Name = "Test Product",
                        Description = "desc",
                        Price = 249.90m,
                        SKU = "SKU-1",
                        CategoryId = 1,
                        SellerId = 5
                    }
                }
            ]
        };
    }
}
