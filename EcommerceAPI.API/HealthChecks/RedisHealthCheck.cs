using Microsoft.Extensions.Diagnostics.HealthChecks;
using StackExchange.Redis;

namespace EcommerceAPI.API.HealthChecks;

public sealed class RedisHealthCheck(IConnectionMultiplexer redis) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var db = redis.GetDatabase();
            await db.PingAsync();
            return HealthCheckResult.Healthy("Redis erişilebilir.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Redis erişilemiyor.", ex);
        }
    }
}
