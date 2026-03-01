using System.Text.Json;
using EcommerceAPI.Core.Interfaces;
using EcommerceAPI.Core.Utilities.Redis;
using StackExchange.Redis;

namespace EcommerceAPI.Infrastructure.Services;

public sealed class RedisRecommendationCacheService : IRecommendationCacheService
{
    private const int RecentViewHistoryLength = 12;
    private static readonly TimeSpan RecentViewTtl = TimeSpan.FromDays(14);
    private static readonly TimeSpan AlsoViewedTtl = TimeSpan.FromDays(30);
    private readonly IConnectionMultiplexer _redis;

    public RedisRecommendationCacheService(IConnectionMultiplexer redis)
    {
        _redis = redis;
    }

    public async Task TrackProductViewAsync(int productId, int? userId, string? sessionId, CancellationToken cancellationToken = default)
    {
        if (productId <= 0)
        {
            return;
        }

        var scope = ResolveScope(userId, sessionId);
        if (scope == null)
        {
            return;
        }

        var db = _redis.GetDatabase();
        var recentKey = RedisKeys.RecommendationRecentlyViewed(scope);
        var recentItems = await db.ListRangeAsync(recentKey, 0, RecentViewHistoryLength - 1);

        var relatedProductIds = recentItems
            .Select(item => int.TryParse(item, out var parsedId) ? parsedId : 0)
            .Where(otherProductId => otherProductId > 0 && otherProductId != productId)
            .Distinct()
            .Take(RecentViewHistoryLength)
            .ToList();

        foreach (var relatedProductId in relatedProductIds)
        {
            await db.SortedSetIncrementAsync(RedisKeys.RecommendationAlsoViewed(productId), relatedProductId, 1);
            await db.SortedSetIncrementAsync(RedisKeys.RecommendationAlsoViewed(relatedProductId), productId, 1);
            await db.KeyExpireAsync(RedisKeys.RecommendationAlsoViewed(productId), AlsoViewedTtl);
            await db.KeyExpireAsync(RedisKeys.RecommendationAlsoViewed(relatedProductId), AlsoViewedTtl);
        }

        await db.ListRemoveAsync(recentKey, productId, 0);
        await db.ListLeftPushAsync(recentKey, productId);
        await db.ListTrimAsync(recentKey, 0, RecentViewHistoryLength - 1);
        await db.KeyExpireAsync(recentKey, RecentViewTtl);
    }

    public async Task<IReadOnlyList<int>> GetAlsoViewedProductIdsAsync(int productId, int take, CancellationToken cancellationToken = default)
    {
        var db = _redis.GetDatabase();
        var values = await db.SortedSetRangeByRankAsync(
            RedisKeys.RecommendationAlsoViewed(productId),
            0,
            Math.Max(take, 1) - 1,
            Order.Descending);

        return values
            .Select(value => int.TryParse(value, out var parsedId) ? parsedId : 0)
            .Where(id => id > 0 && id != productId)
            .ToList();
    }

    public async Task<IReadOnlyList<int>?> GetFrequentlyBoughtTogetherProductIdsAsync(int productId, CancellationToken cancellationToken = default)
    {
        var db = _redis.GetDatabase();
        var value = await db.StringGetAsync(RedisKeys.RecommendationFrequentlyBought(productId));
        if (!value.HasValue)
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<List<int>>(value!) ?? [];
        }
        catch
        {
            return null;
        }
    }

    public async Task CacheFrequentlyBoughtTogetherProductIdsAsync(int productId, IEnumerable<int> productIds, TimeSpan ttl, CancellationToken cancellationToken = default)
    {
        var db = _redis.GetDatabase();
        var payload = JsonSerializer.Serialize(productIds.Distinct().ToArray());
        await db.StringSetAsync(RedisKeys.RecommendationFrequentlyBought(productId), payload, ttl);
    }

    private static string? ResolveScope(int? userId, string? sessionId)
    {
        if (userId is > 0)
        {
            return $"user:{userId.Value}";
        }

        if (!string.IsNullOrWhiteSpace(sessionId))
        {
            return $"session:{sessionId.Trim()}";
        }

        return null;
    }
}
