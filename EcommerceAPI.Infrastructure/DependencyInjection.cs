using EcommerceAPI.Business.Abstract;
using EcommerceAPI.Business.Concrete;

using EcommerceAPI.Core.CrossCuttingConcerns.Logging;
using EcommerceAPI.Core.CrossCuttingConcerns;
using EcommerceAPI.Core.Interfaces;
using EcommerceAPI.Infrastructure.ExternalServices;
using EcommerceAPI.Infrastructure.Services;
using EcommerceAPI.Infrastructure.Settings;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace EcommerceAPI.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration,
        string environment)
    {
        var redisEnv = Environment.GetEnvironmentVariable("REDIS_CONNECTION_STRING");
        var redisConfig = configuration["Redis:ConnectionString"];
        var redisConnection = "localhost:6379";
        var elasticsearchUrl = Environment.GetEnvironmentVariable("ELASTICSEARCH_URL")
                                ?? configuration["Elasticsearch:Url"]
                                ?? "http://localhost:9200";

        if (!string.IsNullOrWhiteSpace(redisEnv))
        {
            redisConnection = redisEnv;
        }
        else if (!string.IsNullOrWhiteSpace(redisConfig))
        {
            redisConnection = redisConfig;
        }
        if (environment == "Test")
        {
            redisConnection = "localhost:6379";
        }

        var redis = ConnectionMultiplexer.Connect(redisConnection);
        services.AddSingleton<IConnectionMultiplexer>(redis);

        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = redisConnection;
            options.InstanceName = "EcommerceAPI:";
        });

        services.AddHttpClient("elasticsearch", client =>
        {
            client.BaseAddress = new Uri(elasticsearchUrl);
            client.Timeout = TimeSpan.FromSeconds(5);
        });

        services.Configure<IyzicoSettings>(options =>
        {
            var config = configuration.GetSection("Iyzico");
            options.ApiKey = Environment.GetEnvironmentVariable("IYZICO_API_KEY")
                             ?? config["ApiKey"]
                             ?? string.Empty;
            options.SecretKey = Environment.GetEnvironmentVariable("IYZICO_SECRET_KEY")
                                ?? config["SecretKey"]
                                ?? string.Empty;
            options.BaseUrl = Environment.GetEnvironmentVariable("IYZICO_BASE_URL")
                              ?? config["BaseUrl"]
                              ?? "https://sandbox-api.iyzipay.com";
        });

        services.Configure<EmailNotificationSettings>(options =>
        {
            var config = configuration.GetSection("EmailNotifications");
            options.Enabled = bool.TryParse(Environment.GetEnvironmentVariable("EMAIL_NOTIFICATIONS_ENABLED"), out var enabled)
                ? enabled
                : config.GetValue("Enabled", false);
            options.Host = Environment.GetEnvironmentVariable("EMAIL_SMTP_HOST")
                           ?? config["Host"]
                           ?? string.Empty;
            options.Port = int.TryParse(Environment.GetEnvironmentVariable("EMAIL_SMTP_PORT"), out var port)
                ? port
                : config.GetValue("Port", 587);
            options.Username = Environment.GetEnvironmentVariable("EMAIL_SMTP_USERNAME")
                               ?? config["Username"]
                               ?? string.Empty;
            options.Password = Environment.GetEnvironmentVariable("EMAIL_SMTP_PASSWORD")
                               ?? config["Password"]
                               ?? string.Empty;
            options.EnableSsl = bool.TryParse(Environment.GetEnvironmentVariable("EMAIL_SMTP_ENABLE_SSL"), out var enableSsl)
                ? enableSsl
                : config.GetValue("EnableSsl", true);
            options.FromAddress = Environment.GetEnvironmentVariable("EMAIL_FROM_ADDRESS")
                                  ?? config["FromAddress"]
                                  ?? string.Empty;
            options.FromName = Environment.GetEnvironmentVariable("EMAIL_FROM_NAME")
                               ?? config["FromName"]
                               ?? "E-Ticaret";
        });

        services.AddScoped<IPaymentService, IyzicoPaymentService>();
        services.AddScoped<IRefundService, IyzicoRefundService>();
        services.AddScoped<IIyzicoRefundGateway, IyzicoRefundGateway>();
        services.AddScoped<IDistributedLockService, RedisDistributedLockService>();
        services.AddScoped<IRecommendationCacheService, RedisRecommendationCacheService>();
        services.AddScoped<ICartCacheService, RedisCartCacheService>();
        services.AddScoped<IAuditService, ElasticAuditService>();

        services.AddScoped<ICacheService, RedisCacheManager>();
        services.AddScoped<IEncryptionService, EncryptionService>();
        services.AddScoped<IHashingService, HashingService>();
        services.AddScoped<IEmailNotificationService, SmtpEmailNotificationService>();
        services.AddScoped<ITokenHelper, JwtTokenHelper>();
        services.AddSingleton<ICorrelationIdProvider, CorrelationIdProvider>();

        services.AddScoped<IProductSearchIndexService, ElasticProductSearchIndexService>();

        return services;
    }

    public static IConnectionMultiplexer GetRedisConnection(this IServiceProvider services)
    {
        return services.GetRequiredService<IConnectionMultiplexer>();
    }
}
