using EcommerceAPI.Business.Concrete;
using EcommerceAPI.Business.Abstract;
using EcommerceAPI.Core.Interfaces;
using EcommerceAPI.DataAccess.Abstract;
using EcommerceAPI.Entities.Concrete;
using EcommerceAPI.Entities.DTOs;
using EcommerceAPI.Entities.Enums;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace EcommerceAPI.UnitTests;

public class RecommendationManagerTests
{
    private readonly Mock<IRecommendationCacheService> _recommendationCacheServiceMock;
    private readonly Mock<IProductDal> _productDalMock;
    private readonly Mock<IOrderDal> _orderDalMock;
    private readonly Mock<IProductSearchIndexService> _productSearchIndexServiceMock;
    private readonly RecommendationManager _manager;

    public RecommendationManagerTests()
    {
        _recommendationCacheServiceMock = new Mock<IRecommendationCacheService>();
        _productDalMock = new Mock<IProductDal>();
        _orderDalMock = new Mock<IOrderDal>();
        _productSearchIndexServiceMock = new Mock<IProductSearchIndexService>();

        _manager = new RecommendationManager(
            _recommendationCacheServiceMock.Object,
            _productDalMock.Object,
            _orderDalMock.Object,
            _productSearchIndexServiceMock.Object,
            Mock.Of<ILogger<RecommendationManager>>());
    }

    [Fact]
    public async Task TrackProductViewAsync_WhenProductExists_ShouldForwardToCache()
    {
        _productDalMock
            .Setup(x => x.GetWithCategoryAsync(55))
            .ReturnsAsync(new Product
            {
                Id = 55,
                Name = "Test",
                Description = "Desc",
                Price = 10,
                SKU = "SKU-55",
                CategoryId = 4,
                Category = new Category { Id = 4, Name = "Elektronik", Description = "x" },
                IsActive = true
            });

        var result = await _manager.TrackProductViewAsync(55, 42, "session-1");

        result.Success.Should().BeTrue();
        _recommendationCacheServiceMock.Verify(x => x.TrackProductViewAsync(55, 42, "session-1", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetFrequentlyBoughtTogetherProductsAsync_WhenCacheMiss_ShouldUseOrderDataAndCacheIt()
    {
        _productDalMock
            .Setup(x => x.GetWithCategoryAsync(10))
            .ReturnsAsync(new Product
            {
                Id = 10,
                Name = "Kaynak Ürün",
                Description = "d",
                Price = 100,
                SKU = "SKU-10",
                CategoryId = 3,
                IsActive = true
            });

        _recommendationCacheServiceMock
            .Setup(x => x.GetFrequentlyBoughtTogetherProductIdsAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<int>?)null);

        _orderDalMock
            .Setup(x => x.GetFrequentlyBoughtTogetherProductIdsAsync(10, It.IsAny<int>()))
            .ReturnsAsync([99, 98]);

        _productDalMock
            .Setup(x => x.GetByIdsWithInventoryAsync(It.IsAny<List<int>>()))
            .ReturnsAsync([
                new Product { Id = 99, Name = "Öneri A", Description = "A", Price = 120, SKU = "SKU-99", CategoryId = 3, Category = new Category { Id = 3, Name = "Elektronik", Description = "d" }, IsActive = true },
                new Product { Id = 98, Name = "Öneri B", Description = "B", Price = 130, SKU = "SKU-98", CategoryId = 3, Category = new Category { Id = 3, Name = "Elektronik", Description = "d" }, IsActive = true }
            ]);

        var result = await _manager.GetFrequentlyBoughtTogetherProductsAsync(10, 2);

        result.Success.Should().BeTrue();
        result.Data.Select(x => x.Id).Should().Equal([99, 98]);
        _recommendationCacheServiceMock.Verify(
            x => x.CacheFrequentlyBoughtTogetherProductIdsAsync(10, It.Is<IEnumerable<int>>(ids => ids.SequenceEqual(new[] { 99, 98 })), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetAlsoViewedProductsAsync_WhenRedisEmpty_ShouldUseCategoryFallback()
    {
        var currentProduct = new Product
        {
            Id = 11,
            Name = "Kaynak Ürün",
            Description = "d",
            Price = 100,
            SKU = "SKU-11",
            CategoryId = 8,
            IsActive = true
        };

        _productDalMock
            .Setup(x => x.GetWithCategoryAsync(11))
            .ReturnsAsync(currentProduct);

        _recommendationCacheServiceMock
            .Setup(x => x.GetAlsoViewedProductIdsAsync(11, It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        _productDalMock
            .Setup(x => x.GetPagedAsync(1, It.IsAny<int>(), currentProduct.CategoryId, null, null, null, null, "wishlistcount", true))
            .ReturnsAsync((
                new List<Product>
                {
                    new Product { Id = 44, Name = "Fallback A", Description = "A", Price = 50, SKU = "SKU-44", CategoryId = 8, Category = new Category { Id = 8, Name = "Oyuncak", Description = "d" }, IsActive = true },
                    new Product { Id = 45, Name = "Fallback B", Description = "B", Price = 60, SKU = "SKU-45", CategoryId = 8, Category = new Category { Id = 8, Name = "Oyuncak", Description = "d" }, IsActive = true }
                }.AsEnumerable(),
                2));

        var result = await _manager.GetAlsoViewedProductsAsync(11, 2);

        result.Success.Should().BeTrue();
        result.Data.Select(x => x.Id).Should().Equal([44, 45]);
    }

    [Fact]
    public async Task TrackRecommendationClickAsync_WithKnownSource_ShouldSucceed()
    {
        var result = await _manager.TrackRecommendationClickAsync(10, 11, "also-viewed", 42, "session-1");

        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task TrackSearchQueryAsync_WithValidQuery_ShouldForwardToCache()
    {
        var result = await _manager.TrackSearchQueryAsync(77, "oyuncak araba");

        result.Success.Should().BeTrue();
        _recommendationCacheServiceMock.Verify(
            x => x.TrackSearchQueryAsync(77, "oyuncak araba", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetPersonalizedProductsAsync_WhenSignalsExist_ShouldUseSearchIndexService()
    {
        _recommendationCacheServiceMock
            .Setup(x => x.GetWishlistCategoryScoresAsync(42, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<int, double> { [8] = 4, [3] = 2 });

        _recommendationCacheServiceMock
            .Setup(x => x.GetRecentSearchQueriesAsync(42, 5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(["lego", "oyuncak"]);

        _productSearchIndexServiceMock
            .Setup(x => x.GetPersonalizedRecommendationsAsync(
                It.IsAny<IReadOnlyDictionary<int, double>>(),
                It.IsAny<IReadOnlyList<string>>(),
                4,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new ProductDto
                {
                    Id = 501,
                    Name = "Kişisel Öneri",
                    Description = "d",
                    Price = 99,
                    Currency = "TRY",
                    SKU = "REC-501",
                    IsActive = true,
                    CategoryId = 8,
                    CategoryName = "Oyuncak",
                    StockQuantity = 5,
                    WishlistCount = 3
                }
            ]);

        var result = await _manager.GetPersonalizedProductsAsync(42, 4);

        result.Success.Should().BeTrue();
        result.Data.Should().ContainSingle(x => x.Id == 501);
        _productSearchIndexServiceMock.Verify(
            x => x.GetPersonalizedRecommendationsAsync(
                It.Is<IReadOnlyDictionary<int, double>>(scores => scores.Count == 2 && scores[8] == 4),
                It.Is<IReadOnlyList<string>>(queries => queries.SequenceEqual(new[] { "lego", "oyuncak" })),
                4,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
