using EcommerceAPI.Business.Concrete;
using EcommerceAPI.DataAccess.Abstract;
using EcommerceAPI.Entities.Concrete;
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
            .Setup(x => x.GetAllOrdersWithDetailsAsync())
            .ReturnsAsync([
                new Order
                {
                    Id = 1,
                    CreatedAt = today.AddHours(3),
                    Status = OrderStatus.Paid,
                    TotalAmount = 120m,
                    Currency = "TRY"
                },
                new Order
                {
                    Id = 2,
                    CreatedAt = today.AddHours(5),
                    Status = OrderStatus.Cancelled,
                    TotalAmount = 999m,
                    Currency = "TRY"
                },
                new Order
                {
                    Id = 3,
                    CreatedAt = yesterday.AddHours(2),
                    Status = OrderStatus.Delivered,
                    TotalAmount = 80m,
                    Currency = "TRY"
                }
            ]);

        _userDalMock
            .Setup(x => x.GetAdminUsersWithDetailsAsync())
            .ReturnsAsync([
                new User { Id = 1, FirstName = "Bugun", LastName = "Uye", Email = "today@test.com", CreatedAt = today.AddHours(1) },
                new User { Id = 2, FirstName = "Dun", LastName = "Uye", Email = "yesterday@test.com", CreatedAt = yesterday.AddHours(1) }
            ]);

        _productDalMock
            .Setup(x => x.GetAllActiveWithDetailsAsync())
            .ReturnsAsync([
                new Product { Id = 11, Name = "A", Description = "d", Price = 10, SKU = "A", Currency = "TRY", IsActive = true, SellerId = 101 },
                new Product { Id = 12, Name = "B", Description = "d", Price = 20, SKU = "B", Currency = "TRY", IsActive = true, SellerId = 102 },
                new Product { Id = 13, Name = "C", Description = "d", Price = 20, SKU = "C", Currency = "TRY", IsActive = false, SellerId = 103 }
            ]);

        _categoryDalMock
            .Setup(x => x.GetAllWithProductsAsync())
            .ReturnsAsync([
                new Category { Id = 1, Name = "Elektronik" },
                new Category { Id = 2, Name = "Giyim" }
            ]);

        _sellerProfileDalMock
            .Setup(x => x.GetAdminListWithDetailsAsync())
            .ReturnsAsync([
                new SellerProfile
                {
                    Id = 201,
                    BrandName = "Bekleyen",
                    IsVerified = false,
                    User = new User { Id = 10, FirstName = "Pending", LastName = "Seller", Email = "pending@test.com", AccountStatus = UserAccountStatus.Active }
                },
                new SellerProfile
                {
                    Id = 202,
                    BrandName = "Onayli",
                    IsVerified = true,
                    User = new User { Id = 11, FirstName = "Verified", LastName = "Seller", Email = "verified@test.com", AccountStatus = UserAccountStatus.Active }
                },
                new SellerProfile
                {
                    Id = 203,
                    BrandName = "Pasif",
                    IsVerified = false,
                    User = new User { Id = 12, FirstName = "Suspended", LastName = "Seller", Email = "suspended@test.com", AccountStatus = UserAccountStatus.Suspended }
                }
            ]);

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
            .Setup(x => x.GetAllActiveWithDetailsAsync())
            .ReturnsAsync([
                new Product
                {
                    Id = 21,
                    Name = "Kulaklik",
                    Description = "d",
                    Price = 100,
                    SKU = "K1",
                    Seller = new SellerProfile { BrandName = "Ses Magazasi" },
                    Inventory = new Inventory { QuantityAvailable = 4 }
                },
                new Product
                {
                    Id = 22,
                    Name = "Mouse",
                    Description = "d",
                    Price = 80,
                    SKU = "M1",
                    Seller = new SellerProfile { BrandName = "Tekno" },
                    Inventory = new Inventory { QuantityAvailable = 0 }
                },
                new Product
                {
                    Id = 23,
                    Name = "Klavye",
                    Description = "d",
                    Price = 120,
                    SKU = "K2",
                    Seller = new SellerProfile { BrandName = "Tekno" },
                    Inventory = new Inventory { QuantityAvailable = 8 }
                }
            ]);

        var result = await CreateManager().GetLowStockAsync(5);

        result.Success.Should().BeTrue();
        result.Data.Select(item => item.ProductId).Should().Equal(22, 21);
        result.Data.Select(item => item.Stock).Should().Equal(0, 4);
        result.Data.Should().OnlyContain(item => item.Stock <= 5);
    }
}
