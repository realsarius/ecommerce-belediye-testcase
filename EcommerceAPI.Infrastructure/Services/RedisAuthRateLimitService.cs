using EcommerceAPI.Core.Interfaces;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace EcommerceAPI.Infrastructure.Services;

public class RedisAuthRateLimitService : IAuthRateLimitService
{
    private const int ResendVerificationLimit = 1;
    private static readonly TimeSpan ResendVerificationWindow = TimeSpan.FromMinutes(2);

    private const int ForgotPasswordLimit = 3;
    private static readonly TimeSpan ForgotPasswordWindow = TimeSpan.FromHours(1);

    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<RedisAuthRateLimitService> _logger;

    public RedisAuthRateLimitService(
        IConnectionMultiplexer redis,
        ILogger<RedisAuthRateLimitService> logger)
    {
        _redis = redis;
        _logger = logger;
    }

    public Task<(bool Allowed, int RetryAfterSeconds)> TryConsumeResendVerificationAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        var key = $"ratelimit:auth:resend-verification:{userId}";
        return TryConsumeAsync(key, ResendVerificationLimit, ResendVerificationWindow, cancellationToken);
    }

    public Task<(bool Allowed, int RetryAfterSeconds)> TryConsumeForgotPasswordAsync(
        string emailHash,
        CancellationToken cancellationToken = default)
    {
        var key = $"ratelimit:auth:forgot-password:{emailHash}";
        return TryConsumeAsync(key, ForgotPasswordLimit, ForgotPasswordWindow, cancellationToken);
    }

    private async Task<(bool Allowed, int RetryAfterSeconds)> TryConsumeAsync(
        string key,
        int limit,
        TimeSpan window,
        CancellationToken cancellationToken)
    {
        var db = _redis.GetDatabase();
        var count = await db.StringIncrementAsync(key);

        if (count == 1)
        {
            await db.KeyExpireAsync(key, window);
        }

        if (count <= limit)
        {
            return (true, 0);
        }

        var ttl = await db.KeyTimeToLiveAsync(key);
        var retryAfterSeconds = ttl.HasValue
            ? Math.Max(1, (int)Math.Ceiling(ttl.Value.TotalSeconds))
            : (int)window.TotalSeconds;

        _logger.LogWarning(
            "Auth rate limit exceeded. Key={Key}, Count={Count}, RetryAfterSeconds={RetryAfterSeconds}",
            key,
            count,
            retryAfterSeconds);

        return (false, retryAfterSeconds);
    }
}
