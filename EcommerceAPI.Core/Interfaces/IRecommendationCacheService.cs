namespace EcommerceAPI.Core.Interfaces;

public interface IRecommendationCacheService
{
    Task TrackProductViewAsync(int productId, int? userId, string? sessionId, CancellationToken cancellationToken = default);
    Task TrackSearchQueryAsync(int userId, string query, CancellationToken cancellationToken = default);
    Task<IReadOnlyDictionary<int, double>> GetWishlistCategoryScoresAsync(int userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<string>> GetRecentSearchQueriesAsync(int userId, int take = 5, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<int>> GetAlsoViewedProductIdsAsync(int productId, int take, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<int>?> GetFrequentlyBoughtTogetherProductIdsAsync(int productId, CancellationToken cancellationToken = default);
    Task CacheFrequentlyBoughtTogetherProductIdsAsync(int productId, IEnumerable<int> productIds, TimeSpan ttl, CancellationToken cancellationToken = default);
    Task<IReadOnlyDictionary<int, long>> GetProductViewCountsAsync(IEnumerable<int> productIds, CancellationToken cancellationToken = default);
    Task<IReadOnlyDictionary<DateOnly, long>> GetProductViewTrendAsync(IEnumerable<int> productIds, int days, CancellationToken cancellationToken = default);
}
