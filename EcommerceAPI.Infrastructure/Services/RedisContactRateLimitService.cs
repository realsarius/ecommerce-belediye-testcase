using EcommerceAPI.Core.Interfaces;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace EcommerceAPI.Infrastructure.Services;

public class RedisContactRateLimitService : IContactRateLimitService
{
    private static readonly TimeSpan Window = TimeSpan.FromHours(1);
    private const int Limit = 5;

    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<RedisContactRateLimitService> _logger;

    public RedisContactRateLimitService(
        IConnectionMultiplexer redis,
        ILogger<RedisContactRateLimitService> logger)
    {
        _redis = redis;
        _logger = logger;
    }

    public async Task<(bool Allowed, int RetryAfterSeconds)> TryConsumeAsync(string ipAddress, CancellationToken cancellationToken = default)
    {
        var db = _redis.GetDatabase();
        var key = $"ratelimit:contact:{ipAddress}";

        var count = await db.StringIncrementAsync(key);

        if (count == 1)
        {
            await db.KeyExpireAsync(key, Window);
        }

        if (count <= Limit)
        {
            return (true, 0);
        }

        var ttl = await db.KeyTimeToLiveAsync(key);
        var retryAfterSeconds = ttl.HasValue
            ? Math.Max(1, (int)Math.Ceiling(ttl.Value.TotalSeconds))
            : (int)Window.TotalSeconds;

        _logger.LogWarning(
            "Contact rate limit exceeded. IpAddress={IpAddress}, Count={Count}, RetryAfterSeconds={RetryAfterSeconds}",
            ipAddress,
            count,
            retryAfterSeconds);

        return (false, retryAfterSeconds);
    }
}
