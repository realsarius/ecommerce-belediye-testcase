using EcommerceAPI.Business.Abstract;
using EcommerceAPI.Business.Extensions;
using EcommerceAPI.Core.Interfaces;
using EcommerceAPI.Core.Utilities.Results;
using EcommerceAPI.DataAccess.Abstract;
using EcommerceAPI.Entities.Concrete;
using EcommerceAPI.Entities.DTOs;
using Microsoft.Extensions.Logging;

namespace EcommerceAPI.Business.Concrete;

public class RecommendationManager : IRecommendationService
{
    private static readonly TimeSpan FrequentlyBoughtCacheTtl = TimeSpan.FromHours(6);
    private readonly IRecommendationCacheService _recommendationCacheService;
    private readonly IProductDal _productDal;
    private readonly IOrderDal _orderDal;
    private readonly ILogger<RecommendationManager> _logger;

    public RecommendationManager(
        IRecommendationCacheService recommendationCacheService,
        IProductDal productDal,
        IOrderDal orderDal,
        ILogger<RecommendationManager> logger)
    {
        _recommendationCacheService = recommendationCacheService;
        _productDal = productDal;
        _orderDal = orderDal;
        _logger = logger;
    }

    public async Task<IResult> TrackProductViewAsync(int productId, int? userId, string? sessionId, CancellationToken cancellationToken = default)
    {
        var product = await _productDal.GetWithCategoryAsync(productId);
        if (product == null || !product.IsActive)
        {
            return new ErrorResult("Ürün bulunamadı.");
        }

        await _recommendationCacheService.TrackProductViewAsync(productId, userId, sessionId, cancellationToken);

        _logger.LogInformation(
            "Recommendation analytics event. AnalyticsStream={AnalyticsStream}, AnalyticsEvent={AnalyticsEvent}, ProductId={ProductId}, UserId={UserId}, SessionId={SessionId}, Category={Category}, OccurredAt={OccurredAt}",
            "Recommendation",
            "ProductViewed",
            productId,
            userId,
            sessionId,
            product.Category?.Name,
            DateTime.UtcNow);

        return new SuccessResult();
    }

    public Task<IResult> TrackRecommendationClickAsync(int productId, int targetProductId, string source, int? userId, string? sessionId, CancellationToken cancellationToken = default)
    {
        if (productId <= 0 || targetProductId <= 0 || productId == targetProductId)
        {
            return Task.FromResult<IResult>(new ErrorResult("Geçersiz öneri tıklama verisi."));
        }

        var normalizedSource = NormalizeSource(source);
        if (normalizedSource == null)
        {
            return Task.FromResult<IResult>(new ErrorResult("Geçersiz öneri kaynağı."));
        }

        _logger.LogInformation(
            "Recommendation analytics event. AnalyticsStream={AnalyticsStream}, AnalyticsEvent={AnalyticsEvent}, Source={Source}, ProductId={ProductId}, TargetProductId={TargetProductId}, UserId={UserId}, SessionId={SessionId}, OccurredAt={OccurredAt}",
            "Recommendation",
            "RecommendationClicked",
            normalizedSource,
            productId,
            targetProductId,
            userId,
            sessionId,
            DateTime.UtcNow);

        return Task.FromResult<IResult>(new SuccessResult());
    }

    public async Task<IDataResult<List<ProductDto>>> GetAlsoViewedProductsAsync(int productId, int take = 4, CancellationToken cancellationToken = default)
    {
        var currentProduct = await _productDal.GetWithCategoryAsync(productId);
        if (currentProduct == null || !currentProduct.IsActive)
        {
            return new ErrorDataResult<List<ProductDto>>("Ürün bulunamadı.");
        }

        var ids = await _recommendationCacheService.GetAlsoViewedProductIdsAsync(productId, Math.Max(take, 1) * 3, cancellationToken);
        var products = ids.Count > 0
            ? await LoadProductsByIdsPreservingOrderAsync(ids, productId, take)
            : await GetCategoryFallbackAsync(currentProduct, take);

        _logger.LogInformation(
            "Recommendation analytics event. AnalyticsStream={AnalyticsStream}, AnalyticsEvent={AnalyticsEvent}, Source={Source}, ProductId={ProductId}, ResultCount={ResultCount}, FallbackUsed={FallbackUsed}, OccurredAt={OccurredAt}",
            "Recommendation",
            "RecommendationServed",
            "AlsoViewed",
            productId,
            products.Count,
            ids.Count == 0,
            DateTime.UtcNow);

        return new SuccessDataResult<List<ProductDto>>(products.Select(p => p.ToDto()).ToList());
    }

    public async Task<IDataResult<List<ProductDto>>> GetFrequentlyBoughtTogetherProductsAsync(int productId, int take = 4, CancellationToken cancellationToken = default)
    {
        var currentProduct = await _productDal.GetWithCategoryAsync(productId);
        if (currentProduct == null || !currentProduct.IsActive)
        {
            return new ErrorDataResult<List<ProductDto>>("Ürün bulunamadı.");
        }

        var cachedIds = await _recommendationCacheService.GetFrequentlyBoughtTogetherProductIdsAsync(productId, cancellationToken);
        IReadOnlyList<int> ids;
        var usedFallback = false;

        if (cachedIds is { Count: > 0 })
        {
            ids = cachedIds;
        }
        else
        {
            ids = await _orderDal.GetFrequentlyBoughtTogetherProductIdsAsync(productId, Math.Max(take, 1) * 3);
            if (ids.Count > 0)
            {
                await _recommendationCacheService.CacheFrequentlyBoughtTogetherProductIdsAsync(productId, ids, FrequentlyBoughtCacheTtl, cancellationToken);
            }
        }

        var products = ids.Count > 0
            ? await LoadProductsByIdsPreservingOrderAsync(ids, productId, take)
            : await GetCategoryFallbackAsync(currentProduct, take);

        if (ids.Count == 0)
        {
            usedFallback = true;
        }

        _logger.LogInformation(
            "Recommendation analytics event. AnalyticsStream={AnalyticsStream}, AnalyticsEvent={AnalyticsEvent}, Source={Source}, ProductId={ProductId}, ResultCount={ResultCount}, FallbackUsed={FallbackUsed}, OccurredAt={OccurredAt}",
            "Recommendation",
            "RecommendationServed",
            "FrequentlyBoughtTogether",
            productId,
            products.Count,
            usedFallback,
            DateTime.UtcNow);

        return new SuccessDataResult<List<ProductDto>>(products.Select(p => p.ToDto()).ToList());
    }

    private async Task<List<Product>> LoadProductsByIdsPreservingOrderAsync(IEnumerable<int> ids, int currentProductId, int take)
    {
        var orderedIds = ids.Where(id => id != currentProductId).Distinct().ToList();
        var products = await _productDal.GetByIdsWithInventoryAsync(orderedIds);
        var productMap = products
            .Where(product => product.IsActive)
            .ToDictionary(product => product.Id);

        return orderedIds
            .Where(productMap.ContainsKey)
            .Select(id => productMap[id])
            .Take(take)
            .ToList();
    }

    private async Task<List<Product>> GetCategoryFallbackAsync(Product currentProduct, int take)
    {
        var (items, _) = await _productDal.GetPagedAsync(
            page: 1,
            pageSize: Math.Max(take, 1) + 4,
            categoryId: currentProduct.CategoryId,
            sortBy: "wishlistcount",
            sortDescending: true);

        return items
            .Where(product => product.Id != currentProduct.Id)
            .Take(take)
            .ToList();
    }

    private static string? NormalizeSource(string source)
    {
        if (string.IsNullOrWhiteSpace(source))
        {
            return null;
        }

        var normalized = source.Trim().ToLowerInvariant();
        return normalized switch
        {
            "also-viewed" => "AlsoViewed",
            "frequently-bought" => "FrequentlyBoughtTogether",
            _ => null
        };
    }
}
