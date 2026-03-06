using EcommerceAPI.API.Hubs;
using EcommerceAPI.API.Middleware;
using EcommerceAPI.Application.Abstractions.ServiceContracts;
using EcommerceAPI.Core.Interfaces;
using EcommerceAPI.DataAccess.Concrete.EntityFramework.Contexts;
using EcommerceAPI.Seeder;
using Hangfire;
using Hangfire.Dashboard;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Serilog;
using System.Diagnostics;
using System.Text.Json;

namespace EcommerceAPI.API.Extensions;

public static class WebApplicationExtensions
{
    public static async Task ApplyDataInitializationAsync(this WebApplication app, IConfiguration configuration)
    {
        using var scope = app.Services.CreateScope();
        var services = scope.ServiceProvider;

        try
        {
            var context = services.GetRequiredService<AppDbContext>();
            await context.Database.MigrateAsync();

            if (app.Environment.IsDevelopment() || app.Environment.IsEnvironment("Test"))
            {
                var logger = services.GetRequiredService<ILogger<SeedRunner>>();
                var hashingService = services.GetRequiredService<IHashingService>();
                var encryptionService = services.GetRequiredService<IEncryptionService>();

                var seedPath = ResolveSeedDataPath(
                    app.Environment.ContentRootPath,
                    AppContext.BaseDirectory,
                    Directory.GetCurrentDirectory());

                logger.LogInformation("📁 Seed data path: {SeedPath}, Exists: {Exists}", seedPath, Directory.Exists(seedPath));

                var seeder = new SeedRunner(context, logger, seedPath, hashingService, encryptionService);
                await seeder.RunAsync(seed: true);
            }

            if (!app.Environment.IsEnvironment("Test"))
            {
                var backfillService = services.GetRequiredService<IPlatformProductBackfillService>();
                var logger = services.GetRequiredService<ILogger<Program>>();

                if (IsPlatformBackfillSnapshotEnabled(configuration))
                {
                    var snapshotProductIds = await backfillService.GetProductIdsWithoutSellerSnapshotAsync();
                    if (snapshotProductIds.Count > 0)
                    {
                        var snapshotPath = ResolvePlatformBackfillSnapshotPath(
                            configuration,
                            app.Environment.ContentRootPath);

                        await WritePlatformBackfillSnapshotAsync(snapshotPath, snapshotProductIds, logger);
                    }
                }

                var backfillResult = await backfillService.BackfillMissingSellerIdsAsync();
                if (!backfillResult.Success)
                {
                    logger.LogWarning(
                        "Platform product backfill tamamlanamadi. Message={Message}",
                        backfillResult.Message);
                }
            }
        }
        catch (Exception ex)
        {
            var logger = services.GetRequiredService<ILogger<Program>>();
            logger.LogError(ex, "An error occurred while migrating or seeding the database");
        }
    }

    public static void ConfigureRecurringJobs(this WebApplication app, bool hangfireEnabled)
    {
        if (!hangfireEnabled)
        {
            return;
        }

        using var jobScope = app.Services.CreateScope();
        var recurringJobManager = jobScope.ServiceProvider.GetRequiredService<IRecurringJobManager>();
        recurringJobManager.AddOrUpdate<IOrderService>(
            "cancel-expired-orders",
            service => service.CancelExpiredOrdersAsync(),
            "*/15 * * * *");

        recurringJobManager.AddOrUpdate<ISupportConversationService>(
            "auto-close-inactive-support-conversations",
            service => service.AutoCloseInactiveConversationsAsync(),
            "*/15 * * * *");

        recurringJobManager.AddOrUpdate<IWishlistPriceAlertService>(
            "wishlist-price-alert-checker",
            service => service.ProcessPriceAlertsAsync(),
            Cron.Hourly());

        recurringJobManager.AddOrUpdate<IRecommendationService>(
            "recommendation-frequently-bought-warmup",
            service => service.WarmFrequentlyBoughtRecommendationsAsync(),
            Cron.Daily());

        recurringJobManager.AddOrUpdate<ICampaignService>(
            "campaign-lifecycle-sync",
            service => service.ProcessCampaignLifecycleAsync(),
            "*/10 * * * *");

        recurringJobManager.AddOrUpdate<IAuthTokenCleanupService>(
            "cleanup-expired-auth-tokens",
            service => service.ExecuteAsync(),
            Cron.Daily());

        recurringJobManager.AddOrUpdate<IOrphanMediaCleanupService>(
            "cleanup-orphan-media-objects",
            service => service.ExecuteAsync(),
            Cron.Daily(3));
    }

    public static void UseApiRequestPipeline(this WebApplication app, bool rateLimitingEnabled)
    {
        app.UseCorrelationId();
        app.UseSerilogRequestLogging(options =>
        {
            options.MessageTemplate =
                "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms [TraceId: {TraceId}, SpanId: {SpanId}, CorrelationId: {CorrelationId}]";

            options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
            {
                var currentActivity = Activity.Current;
                if (currentActivity is not null)
                {
                    diagnosticContext.Set("TraceId", currentActivity.TraceId.ToString());
                    diagnosticContext.Set("SpanId", currentActivity.SpanId.ToString());
                }

                diagnosticContext.Set(
                    "CorrelationId",
                    httpContext.Items["CorrelationId"]?.ToString() ?? httpContext.TraceIdentifier);
            };
        });
        app.UseExceptionHandling();

        app.Use(async (context, next) =>
        {
            context.Response.Headers.TryAdd("X-Content-Type-Options", "nosniff");
            context.Response.Headers.TryAdd("X-Frame-Options", "DENY");
            context.Response.Headers.TryAdd("Referrer-Policy", "no-referrer");
            context.Response.Headers.TryAdd("Permissions-Policy", "geolocation=(), microphone=(), camera=()");

            await next();
        });

        app.UseCors("AllowFrontend");
        app.UseHttpsRedirection();

        if (rateLimitingEnabled)
        {
            app.UseRateLimiter();
        }

        app.UseAuthentication();
        app.UseAuthorization();
    }

    public static void MapApiEndpoints(this WebApplication app)
    {
        app.MapControllers();

        app.MapHealthChecks("/health/live", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("live")
        });

        app.MapHealthChecks("/health/ready", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("ready")
        });

        app.MapHealthChecks("/health", new HealthCheckOptions
        {
            Predicate = _ => true
        });

        app.MapHub<LiveSupportHub>("/hubs/live-support")
            .RequireRateLimiting("support-hub-connect");
        app.MapHub<WishlistHub>("/hubs/wishlist");
    }

    public static string[] ResolveAllowedOrigins(string environmentName, string? envOrigins, string[]? configuredOrigins)
    {
        var parsedOrigins = (configuredOrigins ?? Array.Empty<string>())
            .Concat((envOrigins ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .Where(origin => !string.IsNullOrWhiteSpace(origin))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (parsedOrigins.Length > 0)
        {
            return parsedOrigins;
        }

        if (string.Equals(environmentName, "Development", StringComparison.OrdinalIgnoreCase))
        {
            return new[]
            {
                "http://localhost:5173",
                "http://localhost:3000",
                "http://localhost:80"
            };
        }

        return new[]
        {
            "http://ecommerce.berkansozer.com",
            "https://ecommerce.berkansozer.com"
        };
    }

    private static string ResolveSeedDataPath(params string[] basePaths)
    {
        var uniqueBasePaths = basePaths
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Distinct(StringComparer.OrdinalIgnoreCase);

        foreach (var basePath in uniqueBasePaths)
        {
            var current = new DirectoryInfo(basePath);

            while (current is not null)
            {
                var candidate = Path.Combine(current.FullName, "seed-data");
                if (Directory.Exists(candidate))
                {
                    return candidate;
                }

                current = current.Parent;
            }
        }

        return Path.Combine(AppContext.BaseDirectory, "seed-data");
    }

    private static bool IsPlatformBackfillSnapshotEnabled(IConfiguration configuration)
    {
        var envValue = Environment.GetEnvironmentVariable("PLATFORM_PRODUCT_BACKFILL_SNAPSHOT_ENABLED");
        if (bool.TryParse(envValue, out var envEnabled))
        {
            return envEnabled;
        }

        return configuration.GetValue("PlatformSeller:BackfillSnapshotEnabled", true);
    }

    private static string ResolvePlatformBackfillSnapshotPath(IConfiguration configuration, string contentRootPath)
    {
        var explicitPath = Environment.GetEnvironmentVariable("PLATFORM_PRODUCT_BACKFILL_SNAPSHOT_PATH")
                           ?? configuration["PlatformSeller:BackfillSnapshotPath"];

        if (!string.IsNullOrWhiteSpace(explicitPath))
        {
            return Path.IsPathRooted(explicitPath)
                ? explicitPath
                : Path.GetFullPath(Path.Combine(contentRootPath, explicitPath));
        }

        var fileName = $"platform-backfill-null-seller-products-{DateTime.UtcNow:yyyyMMdd-HHmmss}.json";
        return Path.Combine(contentRootPath, "logs", "platform-backfill", fileName);
    }

    private static async Task WritePlatformBackfillSnapshotAsync(
        string snapshotPath,
        IReadOnlyList<int> productIds,
        Microsoft.Extensions.Logging.ILogger logger)
    {
        var snapshotDirectory = Path.GetDirectoryName(snapshotPath);
        if (!string.IsNullOrWhiteSpace(snapshotDirectory))
        {
            Directory.CreateDirectory(snapshotDirectory);
        }

        var payload = new
        {
            GeneratedAtUtc = DateTime.UtcNow,
            ProductCount = productIds.Count,
            ProductIds = productIds
        };

        var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        await File.WriteAllTextAsync(snapshotPath, json);

        logger.LogInformation(
            "Platform backfill snapshot yazildi. Path={SnapshotPath}, ProductCount={ProductCount}",
            snapshotPath,
            productIds.Count);
    }
}
