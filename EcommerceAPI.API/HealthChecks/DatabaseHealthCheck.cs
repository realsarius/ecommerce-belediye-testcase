using EcommerceAPI.DataAccess.Concrete.EntityFramework.Contexts;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace EcommerceAPI.API.HealthChecks;

public sealed class DatabaseHealthCheck(AppDbContext dbContext) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var canConnect = await dbContext.Database.CanConnectAsync(cancellationToken);
            return canConnect
                ? HealthCheckResult.Healthy("PostgreSQL erişilebilir.")
                : HealthCheckResult.Unhealthy("PostgreSQL erişilemiyor.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("PostgreSQL health check hatası.", ex);
        }
    }
}
