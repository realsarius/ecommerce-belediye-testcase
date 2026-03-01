namespace EcommerceAPI.Core.Interfaces;

public interface IRecommendationCacheService
{
    Task TrackProductViewAsync(int productId, int? userId, string? sessionId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<int>> GetAlsoViewedProductIdsAsync(int productId, int take, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<int>?> GetFrequentlyBoughtTogetherProductIdsAsync(int productId, CancellationToken cancellationToken = default);
    Task CacheFrequentlyBoughtTogetherProductIdsAsync(int productId, IEnumerable<int> productIds, TimeSpan ttl, CancellationToken cancellationToken = default);
}
