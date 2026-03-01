using EcommerceAPI.Business.Concrete;
using EcommerceAPI.Core.Interfaces;
using EcommerceAPI.DataAccess.Abstract;
using EcommerceAPI.Entities.Concrete;
using EcommerceAPI.Entities.Enums;
using FluentAssertions;
using Moq;

namespace EcommerceAPI.UnitTests;

public class SellerAnalyticsManagerTests
{
    private readonly Mock<IProductDal> _productDalMock = new();
    private readonly Mock<IOrderDal> _orderDalMock = new();
    private readonly Mock<IReturnRequestDal> _returnRequestDalMock = new();
    private readonly Mock<IProductReviewDal> _productReviewDalMock = new();
    private readonly Mock<IWishlistItemDal> _wishlistItemDalMock = new();
    private readonly Mock<IRecommendationCacheService> _recommendationCacheServiceMock = new();

    private SellerAnalyticsManager CreateManager()
    {
        return new SellerAnalyticsManager(
            _productDalMock.Object,
            _orderDalMock.Object,
            _returnRequestDalMock.Object,
            _productReviewDalMock.Object,
            _wishlistItemDalMock.Object,
            _recommendationCacheServiceMock.Object);
    }

    [Fact]
    public async Task GetSummaryAsync_WhenSellerHasProducts_ShouldCalculateCoreMetrics()
    {
        const int sellerId = 8;

        _productDalMock
            .Setup(x => x.GetListAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Product, bool>>>()))
            .ReturnsAsync([
                new Product { Id = 11, Name = "A", Description = "d", Price = 100, SKU = "A", SellerId = sellerId, IsActive = true, WishlistCount = 5, Currency = "TRY" },
                new Product { Id = 12, Name = "B", Description = "d", Price = 150, SKU = "B", SellerId = sellerId, IsActive = false, WishlistCount = 3, Currency = "TRY" }
            ]);

        _orderDalMock
            .Setup(x => x.GetOrdersBySellerIdAsync(sellerId))
            .ReturnsAsync([
                new Order
                {
                    Id = 1001,
                    Status = OrderStatus.Paid,
                    Payment = new Payment { Status = PaymentStatus.Success },
                    OrderItems =
                    [
                        new OrderItem
                        {
                            Product = new Product { Id = 11, Name = "A", Description = "d", Price = 100, SKU = "A", SellerId = sellerId, IsActive = true },
                            Quantity = 2,
                            PriceSnapshot = 100
                        }
                    ]
                }
            ]);

        _productReviewDalMock
            .Setup(x => x.GetListAsync(It.IsAny<System.Linq.Expressions.Expression<Func<ProductReview, bool>>>()))
            .ReturnsAsync([
                new ProductReview { Id = 1, ProductId = 11, Rating = 4, Comment = "iyi" },
                new ProductReview { Id = 2, ProductId = 12, Rating = 5, Comment = "çok iyi" }
            ]);

        _returnRequestDalMock
            .Setup(x => x.GetBySellerIdAsync(sellerId))
            .ReturnsAsync([
                new ReturnRequest { Id = 1, Status = ReturnRequestStatus.Refunded },
                new ReturnRequest { Id = 2, Status = ReturnRequestStatus.Pending }
            ]);

        _recommendationCacheServiceMock
            .Setup(x => x.GetProductViewCountsAsync(It.IsAny<IEnumerable<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<int, long>
            {
                [11] = 80,
                [12] = 20,
            });

        var result = await CreateManager().GetSummaryAsync(sellerId);

        result.Success.Should().BeTrue();
        result.Data.TotalProducts.Should().Be(2);
        result.Data.ActiveProducts.Should().Be(1);
        result.Data.TotalViews.Should().Be(100);
        result.Data.TotalWishlistCount.Should().Be(8);
        result.Data.FavoriteRate.Should().Be(8.00m);
        result.Data.ConversionRate.Should().Be(2.00m);
        result.Data.AverageRating.Should().Be(4.50m);
        result.Data.ReviewCount.Should().Be(2);
        result.Data.SuccessfulOrderCount.Should().Be(1);
        result.Data.ReturnedRequestCount.Should().Be(1);
        result.Data.ReturnRate.Should().Be(100.00m);
        result.Data.GrossRevenue.Should().Be(200.00m);
        result.Data.Currency.Should().Be("TRY");
    }

    [Fact]
    public async Task GetTrendAsync_ShouldMergeViewsFavoritesOrdersAndRatingsIntoDailyPoints()
    {
        const int sellerId = 3;
        var targetDate = DateTime.UtcNow.Date.AddDays(-1);
        var targetDateOnly = DateOnly.FromDateTime(targetDate);

        _productDalMock
            .Setup(x => x.GetListAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Product, bool>>>()))
            .ReturnsAsync([
                new Product { Id = 21, Name = "A", Description = "d", Price = 100, SKU = "A", SellerId = sellerId, IsActive = true }
            ]);

        _recommendationCacheServiceMock
            .Setup(x => x.GetProductViewTrendAsync(It.IsAny<IEnumerable<int>>(), 30, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<DateOnly, long> { [targetDateOnly] = 45 });

        _wishlistItemDalMock
            .Setup(x => x.GetListAsync(It.IsAny<System.Linq.Expressions.Expression<Func<WishlistItem, bool>>>()))
            .ReturnsAsync([
                new WishlistItem { Id = 1, ProductId = 21, WishlistId = 5, AddedAt = targetDate.AddHours(3) },
                new WishlistItem { Id = 2, ProductId = 21, WishlistId = 5, AddedAt = targetDate.AddHours(4) }
            ]);

        _productReviewDalMock
            .Setup(x => x.GetListAsync(It.IsAny<System.Linq.Expressions.Expression<Func<ProductReview, bool>>>()))
            .ReturnsAsync([
                new ProductReview { Id = 1, ProductId = 21, Rating = 5, Comment = "harika", CreatedAt = targetDate.AddHours(2) },
                new ProductReview { Id = 2, ProductId = 21, Rating = 3, Comment = "orta", CreatedAt = targetDate.AddHours(1) }
            ]);

        _orderDalMock
            .Setup(x => x.GetOrdersBySellerIdAsync(sellerId))
            .ReturnsAsync([
                new Order
                {
                    Id = 501,
                    CreatedAt = targetDate.AddHours(6),
                    Status = OrderStatus.Paid,
                    Payment = new Payment { Status = PaymentStatus.Success },
                    OrderItems =
                    [
                        new OrderItem
                        {
                            Product = new Product { Id = 21, Name = "A", Description = "d", Price = 110, SKU = "A", SellerId = sellerId, IsActive = true },
                            Quantity = 1,
                            PriceSnapshot = 110
                        }
                    ]
                }
            ]);

        var result = await CreateManager().GetTrendAsync(sellerId, 30);

        result.Success.Should().BeTrue();
        result.Data.Should().HaveCount(30);

        var point = result.Data.Single(x => x.Date == targetDateOnly);
        point.Views.Should().Be(45);
        point.Favorites.Should().Be(2);
        point.Orders.Should().Be(1);
        point.Revenue.Should().Be(110);
        point.AverageRating.Should().Be(4.00m);
    }
}
