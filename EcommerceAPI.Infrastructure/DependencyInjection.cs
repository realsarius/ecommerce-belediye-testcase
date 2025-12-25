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

        services.AddScoped<IPaymentService, IyzicoPaymentService>();
        services.AddScoped<IDistributedLockService, RedisDistributedLockService>();
        services.AddScoped<ICartCacheService, RedisCartCacheService>();
        services.AddScoped<IAuditService, ElasticAuditService>();

        services.AddScoped<ICacheService, RedisCacheManager>();
        services.AddScoped<IEncryptionService, EncryptionService>();
        services.AddScoped<IHashingService, HashingService>();
        services.AddScoped<ITokenHelper, JwtTokenHelper>();
        services.AddSingleton<ICorrelationIdProvider, CorrelationIdProvider>();

        return services;
    }

    public static IConnectionMultiplexer GetRedisConnection(this IServiceProvider services)
    {
        return services.GetRequiredService<IConnectionMultiplexer>();
    }
}
