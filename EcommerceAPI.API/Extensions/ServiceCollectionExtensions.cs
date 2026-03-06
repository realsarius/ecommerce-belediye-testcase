using EcommerceAPI.API.Authorization;
using EcommerceAPI.API.Consumers;
using EcommerceAPI.API.HealthChecks;
using EcommerceAPI.DataAccess.Concrete.EntityFramework.Contexts;
using MassTransit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using RedisRateLimiting;
using RedisRateLimiting.AspNetCore;
using StackExchange.Redis;
using System.Security.Claims;
using System.Text;
using System.Threading.RateLimiting;

namespace EcommerceAPI.API.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddConfiguredOpenTelemetry(
        this IServiceCollection services,
        bool enabled,
        string serviceName,
        string serviceVersion,
        string environmentName,
        string? otlpEndpoint,
        bool isDevelopment)
    {
        if (!enabled)
        {
            return services;
        }

        var openTelemetryBuilder = services.AddOpenTelemetry()
            .ConfigureResource(resource =>
            {
                resource.AddService(
                    serviceName: serviceName,
                    serviceVersion: serviceVersion);
                resource.AddAttributes(new[]
                {
                    new KeyValuePair<string, object>("deployment.environment", environmentName)
                });
            });

        openTelemetryBuilder.WithTracing(tracing =>
        {
            tracing
                .SetSampler(new AlwaysOnSampler())
                .AddAspNetCoreInstrumentation(options =>
                {
                    options.RecordException = true;
                })
                .AddHttpClientInstrumentation(options =>
                {
                    options.RecordException = true;
                    options.EnrichWithHttpRequestMessage = (activity, request) =>
                    {
                        if (!IsElasticsearchRequest(request.RequestUri))
                        {
                            return;
                        }

                        activity.SetTag("peer.service", "elasticsearch");
                        activity.SetTag("ecommerce.external.system", "elasticsearch");
                    };
                    options.EnrichWithHttpResponseMessage = (activity, response) =>
                    {
                        if (!IsElasticsearchRequest(response.RequestMessage?.RequestUri))
                        {
                            return;
                        }

                        activity.SetTag("ecommerce.elasticsearch.status_code", (int)response.StatusCode);
                    };
                })
                .AddEntityFrameworkCoreInstrumentation(options =>
                {
                    options.SetDbStatementForStoredProcedure = true;
                    options.SetDbStatementForText = isDevelopment;
                })
                .AddRedisInstrumentation(options =>
                {
                    options.SetVerboseDatabaseStatements = isDevelopment;
                })
                .AddSource("MassTransit");

            if (!string.IsNullOrWhiteSpace(otlpEndpoint))
            {
                tracing.AddOtlpExporter(options =>
                {
                    options.Endpoint = new Uri(otlpEndpoint);
                });
            }
        });

        openTelemetryBuilder.WithMetrics(metrics =>
        {
            metrics
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddRuntimeInstrumentation()
                .AddMeter("EcommerceAPI.PaymentWebhook");

            if (!string.IsNullOrWhiteSpace(otlpEndpoint))
            {
                metrics.AddOtlpExporter(options =>
                {
                    options.Endpoint = new Uri(otlpEndpoint);
                });
            }
        });

        return services;
    }

    public static IServiceCollection AddConfiguredMassTransit(
        this IServiceCollection services,
        bool isTestEnvironment,
        string rabbitMqHost,
        int rabbitMqPort,
        string rabbitMqVirtualHost,
        string rabbitMqUsername,
        string rabbitMqPassword,
        int rabbitMqPrefetchCount)
    {
        services.AddMassTransit(configurator =>
        {
            configurator.SetKebabCaseEndpointNameFormatter();
            configurator.AddEntityFrameworkOutbox<AppDbContext>(options =>
            {
                options.UsePostgres();
                options.UseBusOutbox();
            });
            configurator.AddConsumer<OrderCreatedConsumer, OrderCreatedConsumerDefinition>();
            configurator.AddConsumer<AnnouncementCreatedConsumer, AnnouncementCreatedConsumerDefinition>();
            configurator.AddConsumer<OrderStatusChangedConsumer, OrderStatusChangedConsumerDefinition>();
            configurator.AddConsumer<OrderShippedConsumer, OrderShippedConsumerDefinition>();
            configurator.AddConsumer<SellerApplicationReviewedConsumer, SellerApplicationReviewedConsumerDefinition>();
            configurator.AddConsumer<ReturnRequestReviewedConsumer, ReturnRequestReviewedConsumerDefinition>();
            configurator.AddConsumer<ProductIndexSyncConsumer, ProductIndexSyncConsumerDefinition>();
            configurator.AddConsumer<WishlistAnalyticsConsumer, WishlistAnalyticsConsumerDefinition>();
            configurator.AddConsumer<WishlistProductIndexSyncConsumer, WishlistProductIndexSyncConsumerDefinition>();
            configurator.AddConsumer<WishlistPersonalizationConsumer, WishlistPersonalizationConsumerDefinition>();
            configurator.AddConsumer<WishlistPriceAlertNotificationConsumer, WishlistPriceAlertNotificationConsumerDefinition>();
            configurator.AddConsumer<WishlistLowStockNotificationConsumer, WishlistLowStockNotificationConsumerDefinition>();
            configurator.AddConsumer<CampaignStatusChangedConsumer, CampaignStatusChangedConsumerDefinition>();
            configurator.AddConsumer<ContactMessageReceivedConsumer, ContactMessageReceivedConsumerDefinition>();
            if (!isTestEnvironment)
            {
                configurator.AddConsumer<RefundRequestedConsumer, RefundRequestedConsumerDefinition>();
            }

            if (isTestEnvironment)
            {
                configurator.UsingInMemory((context, cfg) =>
                {
                    cfg.ConfigureEndpoints(context);
                });
                return;
            }

            configurator.UsingRabbitMq((context, cfg) =>
            {
                cfg.Host(
                    rabbitMqHost,
                    (ushort)Math.Clamp(rabbitMqPort, 1, 65535),
                    rabbitMqVirtualHost,
                    host =>
                    {
                        host.Username(rabbitMqUsername);
                        host.Password(rabbitMqPassword);
                    });

                cfg.PrefetchCount = (ushort)Math.Clamp(rabbitMqPrefetchCount, 1, 2048);

                cfg.ConfigureEndpoints(context);
            });
        });

        return services;
    }

    public static IServiceCollection AddConfiguredHealthChecks(this IServiceCollection services)
    {
        services.AddHealthChecks()
            .AddCheck("self", () => HealthCheckResult.Healthy(), tags: new[] { "live" })
            .AddCheck<DatabaseHealthCheck>("postgresql", tags: new[] { "ready" })
            .AddCheck<RedisHealthCheck>("redis", tags: new[] { "ready" });

        return services;
    }

    public static IServiceCollection AddConfiguredRateLimiting(
        this IServiceCollection services,
        bool enabled,
        IConnectionMultiplexer redisMultiplexer)
    {
        if (!enabled)
        {
            return services;
        }

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.OnRejected = async (context, token) =>
            {
                var httpContext = context.HttpContext;
                var loggerFactory = httpContext.RequestServices.GetRequiredService<ILoggerFactory>();
                var logger = loggerFactory.CreateLogger("Security.RateLimiting");
                var retryAfterSeconds = 60d;

                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                {
                    retryAfterSeconds = retryAfter.TotalSeconds;
                    httpContext.Response.Headers.RetryAfter =
                        ((int)retryAfter.TotalSeconds).ToString();
                }

                var userId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "anonymous";
                logger.LogWarning(
                    "Rate limit rejected. Endpoint={Endpoint}, Method={Method}, Path={Path}, UserId={UserId}, RemoteIp={RemoteIp}, Authenticated={Authenticated}, RetryAfterSeconds={RetryAfterSeconds}",
                    httpContext.GetEndpoint()?.DisplayName,
                    httpContext.Request.Method,
                    httpContext.Request.Path,
                    userId,
                    httpContext.Connection.RemoteIpAddress?.ToString(),
                    httpContext.User.Identity?.IsAuthenticated == true,
                    retryAfterSeconds);

                httpContext.Response.ContentType = "application/json";
                await httpContext.Response.WriteAsJsonAsync(new
                {
                    errorCode = "RATE_LIMIT_EXCEEDED",
                    message = "Çok fazla istek gönderdiniz. Lütfen bekleyin.",
                    retryAfterSeconds
                }, token);
            };

            ConfigureRedisRateLimiterPolicies(options, redisMultiplexer);
        });

        return services;
    }

    public static IServiceCollection AddConfiguredAuthenticationAndAuthorization(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        var jwtSecretKey = configuration["JWT_SECRET_KEY"]
            ?? throw new InvalidOperationException("JWT_SECRET_KEY environment variable is required. Application cannot start without it.");
        var jwtIssuer = configuration["JWT_ISSUER"]
            ?? throw new InvalidOperationException("JWT_ISSUER environment variable is required. Application cannot start without it.");
        var jwtAudience = configuration["JWT_AUDIENCE"]
            ?? throw new InvalidOperationException("JWT_AUDIENCE environment variable is required. Application cannot start without it.");

        if (Encoding.UTF8.GetByteCount(jwtSecretKey) < 32)
        {
            throw new InvalidOperationException("JWT_SECRET_KEY en az 32 byte olmalıdır.");
        }

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.RequireHttpsMetadata = !environment.IsDevelopment() && !environment.IsEnvironment("Test");
            options.SaveToken = false;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ClockSkew = TimeSpan.Zero,
                ValidIssuer = jwtIssuer,
                ValidAudience = jwtAudience,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecretKey))
            };

            options.Events = new JwtBearerEvents
            {
                OnMessageReceived = context =>
                {
                    var accessToken = context.Request.Query["access_token"];
                    var path = context.HttpContext.Request.Path;

                    if (!string.IsNullOrEmpty(accessToken) && (
                        path.StartsWithSegments("/hubs/live-support") ||
                        path.StartsWithSegments("/hubs/wishlist")))
                    {
                        context.Token = accessToken;
                    }

                    return Task.CompletedTask;
                }
            };
        });

        services.AddAuthorization(options =>
        {
            options.AddPolicy("EmailVerified", policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new EmailVerifiedRequirement());
            });
        });
        services.AddSingleton<IAuthorizationHandler, EmailVerifiedHandler>();

        return services;
    }

    public static IServiceCollection AddConfiguredSwaggerDocumentation(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
                Scheme = "Bearer",
                BearerFormat = "JWT",
                In = Microsoft.OpenApi.Models.ParameterLocation.Header,
                Description = "JWT token giriniz. Örnek: eyJhbGciOiJI..."
            });
            options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
            {
                {
                    new Microsoft.OpenApi.Models.OpenApiSecurityScheme
                    {
                        Reference = new Microsoft.OpenApi.Models.OpenApiReference
                        {
                            Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    Array.Empty<string>()
                }
            });
        });

        return services;
    }

    private static bool IsElasticsearchRequest(Uri? uri)
    {
        if (uri is null)
        {
            return false;
        }

        if (uri.Host.Equals("elasticsearch", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return uri.Port == 9200;
    }

    private static void ConfigureRedisRateLimiterPolicies(RateLimiterOptions options, IConnectionMultiplexer redisMultiplexer)
    {
        AddRedisFixedWindowLimiter(options, "auth", redisMultiplexer, 5, TimeSpan.FromMinutes(1));
        AddRedisSlidingWindowLimiter(options, "payment", redisMultiplexer, 10, TimeSpan.FromMinutes(1));
        AddRedisFixedWindowLimiter(options, "global", redisMultiplexer, 100, TimeSpan.FromMinutes(1));
        AddRedisFixedWindowLimiter(options, "search", redisMultiplexer, 60, TimeSpan.FromMinutes(1));
        AddRedisSlidingWindowLimiter(options, "wishlist", redisMultiplexer, 30, TimeSpan.FromMinutes(1));
        AddRedisFixedWindowLimiter(options, "wishlist-read", redisMultiplexer, 120, TimeSpan.FromMinutes(1));
        AddRedisFixedWindowLimiter(options, "support-message-http", redisMultiplexer, 20, TimeSpan.FromMinutes(1));
        AddRedisFixedWindowLimiter(options, "support-hub-connect", redisMultiplexer, 30, TimeSpan.FromMinutes(1));
    }

    private static void AddRedisFixedWindowLimiter(
        RateLimiterOptions options,
        string policyName,
        IConnectionMultiplexer redisMultiplexer,
        int permitLimit,
        TimeSpan window)
    {
        options.AddRedisFixedWindowLimiter(policyName, limiterOptions =>
        {
            limiterOptions.ConnectionMultiplexerFactory = () => redisMultiplexer;
            limiterOptions.PermitLimit = permitLimit;
            limiterOptions.Window = window;
        });
    }

    private static void AddRedisSlidingWindowLimiter(
        RateLimiterOptions options,
        string policyName,
        IConnectionMultiplexer redisMultiplexer,
        int permitLimit,
        TimeSpan window)
    {
        options.AddRedisSlidingWindowLimiter(policyName, limiterOptions =>
        {
            limiterOptions.ConnectionMultiplexerFactory = () => redisMultiplexer;
            limiterOptions.PermitLimit = permitLimit;
            limiterOptions.Window = window;
        });
    }
}
