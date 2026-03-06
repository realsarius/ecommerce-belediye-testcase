using EcommerceAPI.Business.Concrete;
using EcommerceAPI.Application.Abstractions.Persistence;
using EcommerceAPI.Entities.Concrete;
using EcommerceAPI.Entities.Enums;
using FluentAssertions;
using Moq;

namespace EcommerceAPI.UnitTests;

public class AdminFinanceManagerTests
{
    [Fact]
    public async Task GetSummaryAsync_WhenPlatformSellerHasRevenueAndRefund_ShouldAggregateSellerRowsCorrectly()
    {
        var platformSellerId = 700;
        var productId = 9001;

        var sellerProfiles = new List<SellerProfile>
        {
            new()
            {
                Id = platformSellerId,
                BrandName = "Platform Store",
                Products = new List<Product>
                {
                    new()
                    {
                        Id = productId,
                        SellerId = platformSellerId,
                        Name = "Platform Product"
                    }
                }
            }
        };

        var orders = new List<Order>
        {
            new()
            {
                Id = 1,
                CreatedAt = DateTime.UtcNow,
                Status = OrderStatus.Paid,
                Currency = "TRY",
                OrderItems = new List<OrderItem>
                {
                    new()
                    {
                        ProductId = productId,
                        Quantity = 2,
                        PriceSnapshot = 100m,
                        Product = new Product
                        {
                            Id = productId,
                            Name = "Platform Product"
                        }
                    }
                }
            },
            new()
            {
                Id = 2,
                CreatedAt = DateTime.UtcNow,
                Status = OrderStatus.Refunded,
                Currency = "TRY",
                OrderItems = new List<OrderItem>
                {
                    new()
                    {
                        ProductId = productId,
                        Quantity = 1,
                        PriceSnapshot = 50m,
                        Product = new Product
                        {
                            Id = productId,
                            Name = "Platform Product"
                        }
                    }
                }
            }
        };

        var orderDalMock = new Mock<IOrderDal>();
        orderDalMock
            .Setup(dal => dal.GetAllOrdersWithDetailsAsync())
            .ReturnsAsync(orders);

        var sellerProfileDalMock = new Mock<ISellerProfileDal>();
        sellerProfileDalMock
            .Setup(dal => dal.GetAdminListWithDetailsAsync())
            .ReturnsAsync(sellerProfiles);

        var manager = new AdminFinanceManager(orderDalMock.Object, sellerProfileDalMock.Object);

        var result = await manager.GetSummaryAsync();

        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data.TotalRevenue.Should().Be(200m);
        result.Data.TotalRefundAmount.Should().Be(50m);
        result.Data.TotalCommission.Should().Be(15m);
        result.Data.AverageOrderValue.Should().Be(200m);
        result.Data.SuccessfulOrderCount.Should().Be(1);

        var platformRow = result.Data.Sellers.Single(row => row.SellerId == platformSellerId);
        platformRow.SellerName.Should().Be("Platform Store");
        platformRow.GrossSales.Should().Be(200m);
        platformRow.RefundedAmount.Should().Be(50m);
        platformRow.NetSales.Should().Be(150m);
        platformRow.CommissionRate.Should().Be(10m);
        platformRow.CommissionAmount.Should().Be(15m);
        platformRow.NetEarnings.Should().Be(135m);
        platformRow.SuccessfulOrders.Should().Be(1);
    }
}
