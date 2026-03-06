using EcommerceAPI.Business.Abstract;

using EcommerceAPI.Core.CrossCuttingConcerns.Logging;
using EcommerceAPI.Core.CrossCuttingConcerns;
using EcommerceAPI.Core.CrossCuttingConcerns.Caching;
using EcommerceAPI.Core.Interfaces;
using EcommerceAPI.Entities.Settings;
using EcommerceAPI.Infrastructure.ExternalServices;
using EcommerceAPI.Infrastructure.Services;
using EcommerceAPI.Infrastructure.Settings;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Resend;
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

        var redisConfiguration = ConfigurationOptions.Parse(redisConnection);
        redisConfiguration.AbortOnConnectFail = false;
        redisConfiguration.ConnectRetry = 3;
        redisConfiguration.ReconnectRetryPolicy = new ExponentialRetry(5_000);

        services.TryAddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(redisConfiguration));

        services.AddStackExchangeRedisCache(options =>
        {
            options.ConfigurationOptions = redisConfiguration;
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

        services.Configure<PaymentSettings>(options =>
        {
            configuration.GetSection("PaymentSettings").Bind(options);

            var publicApiBaseUrl = Environment.GetEnvironmentVariable("PUBLIC_API_BASE_URL");
            if (!string.IsNullOrWhiteSpace(publicApiBaseUrl))
            {
                options.PublicApiBaseUrl = publicApiBaseUrl.Trim();
            }

            if (bool.TryParse(
                    Environment.GetEnvironmentVariable("PAYMENT_REQUIRE_WEBHOOK_SIGNATURE"),
                    out var requireWebhookSignature))
            {
                options.RequireWebhookSignature = requireWebhookSignature;
            }

            if (bool.TryParse(
                    Environment.GetEnvironmentVariable("PAYMENT_ALLOW_WEBHOOK_SIGNATURE_BYPASS"),
                    out var allowWebhookSignatureBypass))
            {
                options.AllowWebhookSignatureBypass = allowWebhookSignatureBypass;
            }

            if (options.ActiveProviders == null || options.ActiveProviders.Count == 0)
            {
                options.ActiveProviders = new List<Entities.Enums.PaymentProviderType>
                {
                    Entities.Enums.PaymentProviderType.Iyzico
                };
            }

            if (!options.ActiveProviders.Contains(options.DefaultProvider))
            {
                options.DefaultProvider = options.ActiveProviders[0];
            }
        });
        services.AddSingleton<IPaymentFeaturePolicy, PaymentFeaturePolicy>();

        services.Configure<ReturnAttachmentSettings>(options =>
        {
            configuration.GetSection("ReturnAttachments").Bind(options);
        });

        services.Configure<RefundRetrySettings>(options =>
        {
            configuration.GetSection("RefundRetry").Bind(options);
        });

        services.Configure<FrontendFeatureSettings>(options =>
        {
            configuration.GetSection("FrontendFeatures").Bind(options);

            if (bool.TryParse(
                    Environment.GetEnvironmentVariable("FRONTEND_FEATURE_ENABLE_ADMIN_PRODUCT_IMAGE_UPLOADER"),
                    out var enableAdminProductImageUploader))
            {
                options.EnableAdminProductImageUploader = enableAdminProductImageUploader;
            }

            if (bool.TryParse(
                    Environment.GetEnvironmentVariable("FRONTEND_FEATURE_ENABLE_ADMIN_PRODUCT_SELLER_PICKER"),
                    out var enableAdminProductSellerPicker))
            {
                options.EnableAdminProductSellerPicker = enableAdminProductSellerPicker;
            }
        });

        services.Configure<CloudflareR2Settings>(options =>
        {
            var section = configuration.GetSection("CloudflareR2");

            options.AccountId = Environment.GetEnvironmentVariable("R2_ACCOUNT_ID")
                                ?? section["AccountId"]
                                ?? string.Empty;

            options.AccessKeyId = Environment.GetEnvironmentVariable("R2_ACCESS_KEY_ID")
                                  ?? section["AccessKeyId"]
                                  ?? string.Empty;

            options.SecretAccessKey = Environment.GetEnvironmentVariable("R2_SECRET_ACCESS_KEY")
                                      ?? section["SecretAccessKey"]
                                      ?? string.Empty;

            options.BucketName = Environment.GetEnvironmentVariable("R2_BUCKET_NAME")
                                 ?? section["BucketName"]
                                 ?? string.Empty;

            options.PublicBaseUrl = Environment.GetEnvironmentVariable("R2_PUBLIC_BASE_URL")
                                    ?? section["PublicBaseUrl"]
                                    ?? string.Empty;

            options.PresignedUrlExpirySeconds = int.TryParse(
                Environment.GetEnvironmentVariable("R2_PRESIGNED_URL_EXPIRY_SECONDS"),
                out var presignedExpirySeconds)
                ? presignedExpirySeconds
                : section.GetValue("PresignedUrlExpirySeconds", 300);
        });

        services.Configure<EmailNotificationSettings>(options =>
        {
            var emailConfig = configuration.GetSection("Email");
            var smtpConfig = configuration.GetSection("EmailNotifications");

            options.Provider = Environment.GetEnvironmentVariable("EMAIL_PROVIDER")
                               ?? emailConfig["Provider"]
                               ?? smtpConfig["Provider"]
                               ?? "Smtp";

            options.Enabled = bool.TryParse(Environment.GetEnvironmentVariable("EMAIL_NOTIFICATIONS_ENABLED"), out var enabled)
                ? enabled
                : emailConfig.GetValue<bool?>("Enabled")
                  ?? smtpConfig.GetValue("Enabled", false);

            options.ResendApiKey = Environment.GetEnvironmentVariable("EMAIL_RESEND_API_KEY")
                                   ?? emailConfig["ResendApiKey"]
                                   ?? string.Empty;

            options.BaseUrl = Environment.GetEnvironmentVariable("EMAIL_BASE_URL")
                              ?? emailConfig["BaseUrl"]
                              ?? configuration["Auth:FrontendBaseUrl"]
                              ?? string.Empty;

            options.Host = Environment.GetEnvironmentVariable("EMAIL_SMTP_HOST")
                           ?? smtpConfig["Host"]
                           ?? string.Empty;

            options.Port = int.TryParse(Environment.GetEnvironmentVariable("EMAIL_SMTP_PORT"), out var port)
                ? port
                : smtpConfig.GetValue("Port", 587);

            options.Username = Environment.GetEnvironmentVariable("EMAIL_SMTP_USERNAME")
                               ?? smtpConfig["Username"]
                               ?? string.Empty;

            options.Password = Environment.GetEnvironmentVariable("EMAIL_SMTP_PASSWORD")
                               ?? smtpConfig["Password"]
                               ?? string.Empty;

            options.EnableSsl = bool.TryParse(Environment.GetEnvironmentVariable("EMAIL_SMTP_ENABLE_SSL"), out var enableSsl)
                ? enableSsl
                : smtpConfig.GetValue("EnableSsl", true);

            options.FromAddress = Environment.GetEnvironmentVariable("EMAIL_FROM_ADDRESS")
                                  ?? emailConfig["FromAddress"]
                                  ?? smtpConfig["FromAddress"]
                                  ?? string.Empty;

            options.FromName = Environment.GetEnvironmentVariable("EMAIL_FROM_NAME")
                               ?? emailConfig["FromName"]
                               ?? smtpConfig["FromName"]
                               ?? "E-Ticaret";
        });

        services.AddHttpClient<ResendClient>();
        services.Configure<ResendClientOptions>(options =>
        {
            options.ApiToken = Environment.GetEnvironmentVariable("EMAIL_RESEND_API_KEY")
                               ?? configuration["Email:ResendApiKey"]
                               ?? string.Empty;
        });
        services.AddScoped<IResend, ResendClient>();

        services.AddScoped<IyzicoPaymentService>();
        services.AddScoped<StripePaymentProvider>();
        services.AddScoped<PayTrPaymentProvider>();
        services.AddScoped<IPaymentProvider>(serviceProvider => serviceProvider.GetRequiredService<IyzicoPaymentService>());
        services.AddScoped<IPaymentProvider>(serviceProvider => serviceProvider.GetRequiredService<StripePaymentProvider>());
        services.AddScoped<IPaymentProvider>(serviceProvider => serviceProvider.GetRequiredService<PayTrPaymentProvider>());
        services.AddScoped<IPaymentProviderFactory, PaymentProviderFactory>();
        services.AddScoped<IPaymentService, PaymentService>();

        services.AddScoped<IyzicoRefundService>();
        services.AddScoped<StripeRefundProvider>();
        services.AddScoped<PayTrRefundProvider>();
        services.AddScoped<IRefundProvider>(serviceProvider => serviceProvider.GetRequiredService<IyzicoRefundService>());
        services.AddScoped<IRefundProvider>(serviceProvider => serviceProvider.GetRequiredService<StripeRefundProvider>());
        services.AddScoped<IRefundProvider>(serviceProvider => serviceProvider.GetRequiredService<PayTrRefundProvider>());
        services.AddScoped<IRefundProviderFactory, RefundProviderFactory>();
        services.AddScoped<IRefundService, RefundService>();
        services.AddScoped<IRefundRetryJob, RefundRetryJob>();
        services.AddScoped<IIyzicoRefundGateway, IyzicoRefundGateway>();
        services.AddScoped<IIyzicoPaymentGateway, IyzicoPaymentGateway>();
        services.AddScoped<IDistributedLockService, RedisDistributedLockService>();
        services.AddScoped<IRecommendationCacheService, RedisRecommendationCacheService>();
        services.AddScoped<ICartCacheService, RedisCartCacheService>();
        services.AddScoped<IContactRateLimitService, RedisContactRateLimitService>();
        services.AddScoped<IAuthRateLimitService, RedisAuthRateLimitService>();
        services.AddScoped<IAuditService, ElasticAuditService>();
        services.AddSingleton<ISocialAuthValidator, SocialAuthValidator>();

        services.AddScoped<ICacheService, RedisCacheManager>();
        services.AddSingleton<ICacheManager, RedisAopCacheManager>();
        services.AddScoped<IEncryptionService, EncryptionService>();
        services.AddScoped<IHashingService, HashingService>();
        var emailProvider = ResolveEmailProvider(configuration);
        if (string.Equals(emailProvider, "Resend", StringComparison.OrdinalIgnoreCase))
        {
            services.AddScoped<IEmailNotificationService, ResendEmailNotificationService>();
        }
        else if (string.Equals(emailProvider, "Console", StringComparison.OrdinalIgnoreCase))
        {
            services.AddScoped<IEmailNotificationService, ConsoleEmailNotificationService>();
        }
        else
        {
        services.AddScoped<IEmailNotificationService, SmtpEmailNotificationService>();
        }
        services.AddScoped<IReturnAttachmentStorageService, ReturnAttachmentStorageService>();
        services.AddScoped<IReturnAttachmentAccessService, ReturnAttachmentAccessService>();
        services.AddSingleton<IObjectStorageService, R2ObjectStorageService>();
        services.AddScoped<ITokenHelper, JwtTokenHelper>();
        services.AddSingleton<ICorrelationIdProvider, CorrelationIdProvider>();

        var hangfireEnabled = configuration.GetValue("Hangfire:Enabled", !string.Equals(environment, "Test", StringComparison.OrdinalIgnoreCase));
        if (hangfireEnabled)
        {
            services.AddScoped<IRefundRetryScheduler, HangfireRefundRetryScheduler>();
        }
        else
        {
            services.AddSingleton<IRefundRetryScheduler, NoOpRefundRetryScheduler>();
        }

        services.AddScoped<IProductSearchIndexService, ElasticProductSearchIndexService>();

        return services;
    }

    public static IConnectionMultiplexer GetRedisConnection(this IServiceProvider services)
    {
        return services.GetRequiredService<IConnectionMultiplexer>();
    }

    private static string ResolveEmailProvider(IConfiguration configuration)
    {
        var provider = Environment.GetEnvironmentVariable("EMAIL_PROVIDER")
                       ?? configuration["Email:Provider"]
                       ?? configuration["EmailNotifications:Provider"]
                       ?? "Smtp";

        return provider.Trim();
    }
}
