using System.Text.Json;
using EcommerceAPI.Core.Interfaces;
using EcommerceAPI.Core.Utilities.Redis;
using StackExchange.Redis;

namespace EcommerceAPI.Infrastructure.Services;

public sealed class RedisRecommendationCacheService : IRecommendationCacheService
{
    private const int RecentViewHistoryLength = 12;
    private const int SearchHistoryLength = 10;
    private static readonly TimeSpan RecentViewTtl = TimeSpan.FromDays(14);
    private static readonly TimeSpan AlsoViewedTtl = TimeSpan.FromDays(30);
    private static readonly TimeSpan SearchHistoryTtl = TimeSpan.FromDays(30);
    private static readonly TimeSpan DailyViewTtl = TimeSpan.FromDays(90);
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
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
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
        await db.StringIncrementAsync(RedisKeys.RecommendationProductViews(productId));
        await db.StringIncrementAsync(RedisKeys.RecommendationProductViewsDaily(productId, today));
        await db.KeyExpireAsync(RedisKeys.RecommendationProductViewsDaily(productId, today), DailyViewTtl);
    }

    public async Task TrackSearchQueryAsync(int userId, string query, CancellationToken cancellationToken = default)
    {
        if (userId <= 0 || string.IsNullOrWhiteSpace(query))
        {
            return;
        }

        var normalized = query.Trim();
        if (normalized.Length < 2)
        {
            return;
        }

        var db = _redis.GetDatabase();
        var key = RedisKeys.RecommendationSearchHistory(userId);
        await db.ListRemoveAsync(key, normalized, 0);
        await db.ListLeftPushAsync(key, normalized);
        await db.ListTrimAsync(key, 0, SearchHistoryLength - 1);
        await db.KeyExpireAsync(key, SearchHistoryTtl);
    }

    public async Task<IReadOnlyDictionary<int, double>> GetWishlistCategoryScoresAsync(int userId, CancellationToken cancellationToken = default)
    {
        if (userId <= 0)
        {
            return new Dictionary<int, double>();
        }

        var db = _redis.GetDatabase();
        var entries = await db.HashGetAllAsync(RedisKeys.RecommendationWishlistPreferences(userId));

        return entries
            .Select(entry =>
            {
                var keyParsed = int.TryParse(entry.Name, out var categoryId);
                var valueParsed = double.TryParse(entry.Value, out var score);
                return new { keyParsed, categoryId, valueParsed, score };
            })
            .Where(entry => entry.keyParsed && entry.valueParsed && entry.categoryId > 0 && entry.score > 0)
            .ToDictionary(entry => entry.categoryId, entry => entry.score);
    }

    public async Task<IReadOnlyList<string>> GetRecentSearchQueriesAsync(int userId, int take = 5, CancellationToken cancellationToken = default)
    {
        if (userId <= 0)
        {
            return [];
        }

        var db = _redis.GetDatabase();
        var values = await db.ListRangeAsync(RedisKeys.RecommendationSearchHistory(userId), 0, Math.Max(take, 1) - 1);
        return values
            .Select(value => value.ToString())
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .ToList();
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

    public async Task<IReadOnlyDictionary<int, long>> GetProductViewCountsAsync(IEnumerable<int> productIds, CancellationToken cancellationToken = default)
    {
        var ids = productIds
            .Where(id => id > 0)
            .Distinct()
            .ToArray();

        if (ids.Length == 0)
        {
            return new Dictionary<int, long>();
        }

        var db = _redis.GetDatabase();
        var keys = ids.Select(id => (RedisKey)RedisKeys.RecommendationProductViews(id)).ToArray();
        var values = await db.StringGetAsync(keys);

        var result = new Dictionary<int, long>(ids.Length);
        for (var index = 0; index < ids.Length; index++)
        {
            result[ids[index]] = values[index].HasValue && long.TryParse(values[index], out var count) ? count : 0;
        }

        return result;
    }

    public async Task<IReadOnlyDictionary<DateOnly, long>> GetProductViewTrendAsync(IEnumerable<int> productIds, int days, CancellationToken cancellationToken = default)
    {
        var ids = productIds
            .Where(id => id > 0)
            .Distinct()
            .ToArray();

        if (ids.Length == 0 || days <= 0)
        {
            return new Dictionary<DateOnly, long>();
        }

        var db = _redis.GetDatabase();
        var startDate = DateOnly.FromDateTime(DateTime.UtcNow.Date.AddDays(-(days - 1)));
        var dates = Enumerable.Range(0, days).Select(offset => startDate.AddDays(offset)).ToArray();

        var keyMap = new List<(DateOnly Date, RedisKey Key)>(ids.Length * dates.Length);
        foreach (var date in dates)
        {
            foreach (var productId in ids)
            {
                keyMap.Add((date, RedisKeys.RecommendationProductViewsDaily(productId, date)));
            }
        }

        var values = await db.StringGetAsync(keyMap.Select(entry => entry.Key).ToArray());
        var trend = dates.ToDictionary(date => date, _ => 0L);

        for (var index = 0; index < keyMap.Count; index++)
        {
            if (!values[index].HasValue || !long.TryParse(values[index], out var count))
            {
                continue;
            }

            trend[keyMap[index].Date] += count;
        }

        return trend;
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
