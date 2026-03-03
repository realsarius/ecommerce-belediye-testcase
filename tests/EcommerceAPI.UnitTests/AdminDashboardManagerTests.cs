using EcommerceAPI.Business.Concrete;
using EcommerceAPI.DataAccess.Abstract;
using EcommerceAPI.Entities.DTOs;
using EcommerceAPI.Entities.Enums;
using FluentAssertions;
using Moq;

namespace EcommerceAPI.UnitTests;

public class AdminDashboardManagerTests
{
    private readonly Mock<IOrderDal> _orderDalMock = new();
    private readonly Mock<IUserDal> _userDalMock = new();
    private readonly Mock<IProductDal> _productDalMock = new();
    private readonly Mock<ICategoryDal> _categoryDalMock = new();
    private readonly Mock<ISellerProfileDal> _sellerProfileDalMock = new();

    private AdminDashboardManager CreateManager()
    {
        return new AdminDashboardManager(
            _orderDalMock.Object,
            _userDalMock.Object,
            _productDalMock.Object,
            _categoryDalMock.Object,
            _sellerProfileDalMock.Object);
    }

    [Fact]
    public async Task GetKpiAsync_ShouldCalculateRevenueAndApplicationMetrics()
    {
        var today = DateTime.UtcNow.Date;
        var yesterday = today.AddDays(-1);

        _orderDalMock
            .Setup(x => x.GetAdminDashboardOrderProjectionsAsync())
            .ReturnsAsync([
                new AdminDashboardOrderProjectionDto
                {
                    OrderId = 1,
                    CreatedAt = today.AddHours(3),
                    Status = OrderStatus.Paid,
                    TotalAmount = 120m,
                    Currency = "TRY"
                },
                new AdminDashboardOrderProjectionDto
                {
                    OrderId = 2,
                    CreatedAt = today.AddHours(5),
                    Status = OrderStatus.Cancelled,
                    TotalAmount = 999m,
                    Currency = "TRY"
                },
                new AdminDashboardOrderProjectionDto
                {
                    OrderId = 3,
                    CreatedAt = yesterday.AddHours(2),
                    Status = OrderStatus.Delivered,
                    TotalAmount = 80m,
                    Currency = "TRY"
                }
            ]);

        _userDalMock
            .Setup(x => x.GetAdminDashboardUserCreatedDatesAsync())
            .ReturnsAsync([
                today.AddHours(1),
                yesterday.AddHours(1)
            ]);

        _productDalMock
            .Setup(x => x.GetAdminDashboardProductSummaryAsync())
            .ReturnsAsync((2, 2, "TRY"));

        _categoryDalMock
            .Setup(x => x.GetDashboardCategoryCountAsync())
            .ReturnsAsync(2);

        _sellerProfileDalMock
            .Setup(x => x.GetPendingApplicationCountAsync())
            .ReturnsAsync(1);

        var result = await CreateManager().GetKpiAsync();

        result.Success.Should().BeTrue();
        result.Data.TodayRevenue.Should().Be(120m);
        result.Data.YesterdayRevenue.Should().Be(80m);
        result.Data.TodayOrders.Should().Be(2);
        result.Data.YesterdayOrders.Should().Be(1);
        result.Data.TodayNewUsers.Should().Be(1);
        result.Data.YesterdayNewUsers.Should().Be(1);
        result.Data.ActiveSellers.Should().Be(2);
        result.Data.ActiveProducts.Should().Be(2);
        result.Data.CategoryCount.Should().Be(2);
        result.Data.PendingSellerApplications.Should().Be(1);
        result.Data.Currency.Should().Be("TRY");
    }

    [Fact]
    public async Task GetLowStockAsync_ShouldReturnThresholdFilteredProductsOrderedByStock()
    {
        _productDalMock
            .Setup(x => x.GetAdminDashboardLowStockAsync(5, 5))
            .ReturnsAsync([
                new AdminDashboardLowStockItemDto
                {
                    Name = "Mouse",
                    ProductId = 22,
                    SellerName = "Tekno",
                    Stock = 0
                },
                new AdminDashboardLowStockItemDto
                {
                    Name = "Kulaklik",
                    ProductId = 21,
                    SellerName = "Ses Magazasi",
                    Stock = 4
                }
            ]);

        var result = await CreateManager().GetLowStockAsync(5);

        result.Success.Should().BeTrue();
        result.Data.Select(item => item.ProductId).Should().Equal(22, 21);
        result.Data.Select(item => item.Stock).Should().Equal(0, 4);
        result.Data.Should().OnlyContain(item => item.Stock <= 5);
    }
}
