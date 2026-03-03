using EcommerceAPI.Business.Concrete;
using EcommerceAPI.Business.Abstract;
using EcommerceAPI.Core.CrossCuttingConcerns.Logging;
using EcommerceAPI.Core.Interfaces;
using EcommerceAPI.Core.Utilities.Results;
using EcommerceAPI.DataAccess.Abstract;
using EcommerceAPI.Entities.Concrete;
using EcommerceAPI.Entities.DTOs;
using EcommerceAPI.Entities.Enums;
using EcommerceAPI.Entities.IntegrationEvents;
using FluentAssertions;
using MassTransit;
using Microsoft.Extensions.Logging;
using Moq;

namespace EcommerceAPI.UnitTests;

public class ReturnRequestManagerTests
{
    private readonly Mock<IReturnRequestDal> _returnRequestDalMock;
    private readonly Mock<IRefundRequestDal> _refundRequestDalMock;
    private readonly Mock<IOrderDal> _orderDalMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ILoyaltyService> _loyaltyServiceMock;
    private readonly Mock<IGiftCardService> _giftCardServiceMock;
    private readonly Mock<IReferralService> _referralServiceMock;
    private readonly Mock<IAuditService> _auditServiceMock;
    private readonly Mock<ILogger<ReturnRequestManager>> _loggerMock;
    private readonly Mock<IPublishEndpoint> _publishEndpointMock;
    private readonly ReturnRequestManager _manager;

    public ReturnRequestManagerTests()
    {
        _returnRequestDalMock = new Mock<IReturnRequestDal>();
        _refundRequestDalMock = new Mock<IRefundRequestDal>();
        _orderDalMock = new Mock<IOrderDal>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _loyaltyServiceMock = new Mock<ILoyaltyService>();
        _giftCardServiceMock = new Mock<IGiftCardService>();
        _referralServiceMock = new Mock<IReferralService>();
        _auditServiceMock = new Mock<IAuditService>();
        _loggerMock = new Mock<ILogger<ReturnRequestManager>>();
        _publishEndpointMock = new Mock<IPublishEndpoint>();
        _publishEndpointMock
            .Setup(x => x.Publish(It.IsAny<RefundRequestedEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _publishEndpointMock
            .Setup(x => x.Publish(It.IsAny<ReturnRequestReviewedEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _loyaltyServiceMock
            .Setup(x => x.RestoreRedeemedPointsAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>()))
            .ReturnsAsync(new SuccessResult());
        _loyaltyServiceMock
            .Setup(x => x.ReverseEarnedPointsAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>()))
            .ReturnsAsync(new SuccessResult());
        _giftCardServiceMock
            .Setup(x => x.RestoreForOrderAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>()))
            .ReturnsAsync(new SuccessResult());
        _referralServiceMock
            .Setup(x => x.ReverseRewardsForOrderAsync(It.IsAny<int>(), It.IsAny<string>()))
            .ReturnsAsync(new SuccessResult());

        _manager = new ReturnRequestManager(
            _returnRequestDalMock.Object,
            _refundRequestDalMock.Object,
            _orderDalMock.Object,
            _unitOfWorkMock.Object,
            _loyaltyServiceMock.Object,
            _giftCardServiceMock.Object,
            _referralServiceMock.Object,
            _auditServiceMock.Object,
            _loggerMock.Object,
            _publishEndpointMock.Object);
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
            ReasonCategory = "NotAsDescribed",
            SelectedOrderItemIds = [order.OrderItems.First().Id],
            Reason = "Ürün beklentimi karşılamadı",
            RequestNote = "Kutusu açıldı ama hasarsız."
        });

        result.Success.Should().BeTrue();
        result.Data.Status.Should().Be(ReturnRequestStatus.Pending.ToString());
        result.Data.Type.Should().Be(ReturnRequestType.Return.ToString());
        result.Data.ReasonCategory.Should().Be(ReturnReasonCategory.NotAsDescribed.ToString());
        result.Data.RequestedRefundAmount.Should().Be(order.TotalAmount);
        result.Data.SelectedItems.Should().ContainSingle();
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
            ReasonCategory = "Other",
            Reason = "İade denemesi"
        });

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("teslim edilen");
        _returnRequestDalMock.Verify(x => x.AddAsync(It.IsAny<ReturnRequest>()), Times.Never);
    }

    [Fact]
    public async Task CreateReturnRequestAsync_WhenActiveRequestExists_ShouldFail()
    {
        var order = CreateOrder(OrderStatus.Delivered, PaymentStatus.Success);

        _orderDalMock.Setup(x => x.GetByIdWithDetailsAsync(order.Id))
            .ReturnsAsync(order);
        _returnRequestDalMock.Setup(x => x.HasActiveRequestForOrderAsync(order.Id))
            .ReturnsAsync(true);

        var result = await _manager.CreateReturnRequestAsync(order.UserId, order.Id, new CreateReturnRequestRequest
        {
            Type = "Return",
            ReasonCategory = "Other",
            Reason = "Aynı sipariş için ikinci talep"
        });

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("zaten aktif");
        _returnRequestDalMock.Verify(x => x.AddAsync(It.IsAny<ReturnRequest>()), Times.Never);
    }

    [Fact]
    public async Task CreateReturnRequestAsync_WhenDeliveredMoreThan14DaysAgo_ShouldFail()
    {
        var order = CreateOrder(OrderStatus.Delivered, PaymentStatus.Success);
        order.DeliveredAt = DateTime.UtcNow.Date.AddDays(-15);

        _orderDalMock.Setup(x => x.GetByIdWithDetailsAsync(order.Id))
            .ReturnsAsync(order);
        _returnRequestDalMock.Setup(x => x.HasActiveRequestForOrderAsync(order.Id))
            .ReturnsAsync(false);

        var result = await _manager.CreateReturnRequestAsync(order.UserId, order.Id, new CreateReturnRequestRequest
        {
            Type = "Return",
            ReasonCategory = "ChangedMind",
            Reason = "Süre dışı iade denemesi"
        });

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("14 gün");
        _returnRequestDalMock.Verify(x => x.AddAsync(It.IsAny<ReturnRequest>()), Times.Never);
    }

    [Fact]
    public async Task CreateReturnRequestAsync_WhenMultipleItemsAndNoSelection_ShouldFail()
    {
        var order = CreateOrder(OrderStatus.Delivered, PaymentStatus.Success, includeSecondItem: true);

        _orderDalMock.Setup(x => x.GetByIdWithDetailsAsync(order.Id))
            .ReturnsAsync(order);
        _returnRequestDalMock.Setup(x => x.HasActiveRequestForOrderAsync(order.Id))
            .ReturnsAsync(false);

        var result = await _manager.CreateReturnRequestAsync(order.UserId, order.Id, new CreateReturnRequestRequest
        {
            Type = "Return",
            ReasonCategory = "Other",
            Reason = "Bir urun iade etmek istiyorum"
        });

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("en az bir ürün");
        _returnRequestDalMock.Verify(x => x.AddAsync(It.IsAny<ReturnRequest>()), Times.Never);
    }

    [Fact]
    public async Task CreateReturnRequestAsync_WithSelectedItems_ShouldCalculateRefundAmountFromSelectedItems()
    {
        var order = CreateOrder(OrderStatus.Delivered, PaymentStatus.Success, includeSecondItem: true);

        _orderDalMock.Setup(x => x.GetByIdWithDetailsAsync(order.Id))
            .ReturnsAsync(order);
        _returnRequestDalMock.Setup(x => x.HasActiveRequestForOrderAsync(order.Id))
            .ReturnsAsync(false);
        _returnRequestDalMock.Setup(x => x.AddAsync(It.IsAny<ReturnRequest>()))
            .Callback<ReturnRequest>(request => request.Id = 1002)
            .ReturnsAsync((ReturnRequest request) => request);
        _returnRequestDalMock.Setup(x => x.GetByIdWithDetailsAsync(1002))
            .ReturnsAsync((ReturnRequest?)null);

        var selectedItem = order.OrderItems.Last();
        var expectedRefundAmount = selectedItem.PriceSnapshot * selectedItem.Quantity;

        var result = await _manager.CreateReturnRequestAsync(order.UserId, order.Id, new CreateReturnRequestRequest
        {
            Type = "Return",
            ReasonCategory = "DefectiveDamaged",
            SelectedOrderItemIds = [selectedItem.Id],
            Reason = "Ikinci urun hasarli geldi"
        });

        result.Success.Should().BeTrue();
        result.Data.RequestedRefundAmount.Should().Be(expectedRefundAmount);
        result.Data.SelectedItems.Should().ContainSingle();
        result.Data.SelectedItems[0].OrderItemId.Should().Be(selectedItem.Id);
        result.Data.SelectedItems[0].ProductId.Should().Be(selectedItem.ProductId);
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
        _publishEndpointMock.Verify(x => x.Publish(It.IsAny<RefundRequestedEvent>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(), Times.Exactly(2));
    }

    [Fact]
    public async Task ReviewReturnRequestAsync_ApprovedGiftCardOnlyOrder_ShouldRestoreGiftCardWithoutRefundRequest()
    {
        var order = CreateOrder(OrderStatus.Delivered, PaymentStatus.Success);
        order.TotalAmount = 0m;
        order.GiftCardAmount = 249.90m;
        order.Payment!.Amount = 0m;
        order.Payment.PaymentMethod = "GiftCard";

        var returnRequest = new ReturnRequest
        {
            Id = 3002,
            OrderId = order.Id,
            UserId = 42,
            Type = ReturnRequestType.Return,
            Status = ReturnRequestStatus.Pending,
            Reason = "İade",
            RequestedRefundAmount = 0m,
            Order = order,
            User = new User { Id = 42, FirstName = "Test", LastName = "Customer", Email = "customer@test.com", EmailHash = "hash", PasswordHash = "pw", RoleId = 1 }
        };

        _returnRequestDalMock.Setup(x => x.GetByIdWithDetailsAsync(returnRequest.Id))
            .ReturnsAsync(returnRequest);

        var result = await _manager.ReviewReturnRequestAsync(returnRequest.Id, 7, new ReviewReturnRequestRequest
        {
            Status = "Approved",
            ReviewNote = "Gift card iadesi"
        });

        result.Success.Should().BeTrue();
        result.Data.Status.Should().Be(ReturnRequestStatus.Refunded.ToString());
        _giftCardServiceMock.Verify(x => x.RestoreForOrderAsync(returnRequest.UserId, returnRequest.OrderId, It.IsAny<string>()), Times.Once);
        _referralServiceMock.Verify(x => x.ReverseRewardsForOrderAsync(returnRequest.OrderId, It.IsAny<string>()), Times.Once);
        _refundRequestDalMock.Verify(x => x.AddAsync(It.IsAny<RefundRequest>()), Times.Never);
        _publishEndpointMock.Verify(x => x.Publish(It.IsAny<RefundRequestedEvent>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ReviewReturnRequestAsync_RejectedRequest_ShouldRejectWithoutCreatingRefund()
    {
        var returnRequest = new ReturnRequest
        {
            Id = 3003,
            OrderId = 502,
            UserId = 42,
            Type = ReturnRequestType.Return,
            Status = ReturnRequestStatus.Pending,
            Reason = "Vazgectim",
            RequestedRefundAmount = 149.90m,
            Order = CreateOrder(OrderStatus.Delivered, PaymentStatus.Success),
            User = new User { Id = 42, FirstName = "Test", LastName = "Customer", Email = "customer@test.com", EmailHash = "hash", PasswordHash = "pw", RoleId = 1 }
        };
        returnRequest.Order.Id = 502;

        _returnRequestDalMock.Setup(x => x.GetByIdWithDetailsAsync(returnRequest.Id))
            .ReturnsAsync(returnRequest);

        var result = await _manager.ReviewReturnRequestAsync(returnRequest.Id, 7, new ReviewReturnRequestRequest
        {
            Status = "Rejected",
            ReviewNote = "Kosullar saglanmadi"
        });

        result.Success.Should().BeTrue();
        result.Data.Status.Should().Be(ReturnRequestStatus.Rejected.ToString());
        _refundRequestDalMock.Verify(x => x.AddAsync(It.IsAny<RefundRequest>()), Times.Never);
        _publishEndpointMock.Verify(x => x.Publish(It.IsAny<RefundRequestedEvent>(), It.IsAny<CancellationToken>()), Times.Never);
        _publishEndpointMock.Verify(x => x.Publish(It.IsAny<ReturnRequestReviewedEvent>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task ReviewReturnRequestAsync_WhenSellerDoesNotOwnOrderItems_ShouldReturnError()
    {
        var returnRequest = new ReturnRequest
        {
            Id = 3004,
            OrderId = 503,
            UserId = 42,
            Type = ReturnRequestType.Return,
            Status = ReturnRequestStatus.Pending,
            Reason = "Iade",
            RequestedRefundAmount = 249.90m,
            Order = CreateOrder(OrderStatus.Delivered, PaymentStatus.Success),
            User = new User { Id = 42, FirstName = "Test", LastName = "Customer", Email = "customer@test.com", EmailHash = "hash", PasswordHash = "pw", RoleId = 1 }
        };
        returnRequest.Order.Id = 503;

        _returnRequestDalMock.Setup(x => x.GetByIdWithDetailsAsync(returnRequest.Id))
            .ReturnsAsync(returnRequest);

        var result = await _manager.ReviewReturnRequestAsync(returnRequest.Id, 7, new ReviewReturnRequestRequest
        {
            Status = "Approved",
            ReviewNote = "Yetkisiz deneme"
        }, sellerId: 999);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("ait ürünler");
        _refundRequestDalMock.Verify(x => x.AddAsync(It.IsAny<RefundRequest>()), Times.Never);
        _publishEndpointMock.Verify(x => x.Publish(It.IsAny<RefundRequestedEvent>(), It.IsAny<CancellationToken>()), Times.Never);
        _publishEndpointMock.Verify(x => x.Publish(It.IsAny<ReturnRequestReviewedEvent>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private static Order CreateOrder(OrderStatus orderStatus, PaymentStatus paymentStatus, bool includeSecondItem = false)
    {
        var order = new Order
        {
            Id = 501,
            UserId = 42,
            OrderNumber = "ORD-TEST",
            Status = orderStatus,
            DeliveredAt = orderStatus == OrderStatus.Delivered ? DateTime.UtcNow.Date : null,
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
                    Id = 9101,
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

        if (includeSecondItem)
        {
            order.OrderItems.Add(new OrderItem
            {
                Id = 9102,
                OrderId = order.Id,
                ProductId = 92,
                Quantity = 2,
                PriceSnapshot = 75m,
                Product = new Product
                {
                    Id = 92,
                    Name = "Second Test Product",
                    Description = "desc",
                    Price = 75m,
                    SKU = "SKU-2",
                    CategoryId = 1,
                    SellerId = 5
                }
            });

            order.TotalAmount = order.OrderItems.Sum(item => item.PriceSnapshot * item.Quantity);
            if (order.Payment != null)
            {
                order.Payment.Amount = order.TotalAmount;
            }
        }

        return order;
    }
}
