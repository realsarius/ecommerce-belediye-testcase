using EcommerceAPI.Business;
using EcommerceAPI.Business.Abstract;
using EcommerceAPI.Infrastructure;
using EcommerceAPI.DataAccess.Concrete.EntityFramework.Contexts;
using EcommerceAPI.DataAccess;
using EcommerceAPI.Core.Interfaces;
using Microsoft.EntityFrameworkCore;
using EcommerceAPI.Seeder;
using EcommerceAPI.API.Middleware;
using EcommerceAPI.API.Hubs;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Serilog;
using Hangfire;
using Hangfire.PostgreSql;
using EcommerceAPI.API.Filters;
using RedisRateLimiting;
using RedisRateLimiting.AspNetCore;
using System.Threading.RateLimiting;
using StackExchange.Redis;
using Serilog.Sinks.Elasticsearch;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using EcommerceAPI.Business.DependencyResolvers.Autofac;
using EcommerceAPI.Core.Utilities.IoC;
using MassTransit;
using EcommerceAPI.API.Consumers;
using EcommerceAPI.API.HealthChecks;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.DataProtection;
using EcommerceAPI.API.Services;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using OpenTelemetry.Metrics;
using System.Diagnostics;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseServiceProviderFactory(new AutofacServiceProviderFactory());
builder.Host.ConfigureContainer<ContainerBuilder>(builder =>
{
    builder.RegisterModule(new BusinessModule());
});

var elasticsearchUrl = builder.Configuration["Elasticsearch:Url"]
                       ?? Environment.GetEnvironmentVariable("ELASTICSEARCH_URL")
                       ?? "http://localhost:9200";

builder.Host.UseSerilog((context, configuration) =>
{
    configuration.ReadFrom.Configuration(context.Configuration)
                 .WriteTo.Console();
    
    if (!builder.Environment.IsEnvironment("Test"))
    {
        configuration.WriteTo.Elasticsearch(new ElasticsearchSinkOptions(new Uri(elasticsearchUrl))
        {
            IndexFormat = $"ecommerce-logs-{DateTime.UtcNow:yyyy.MM.dd}",
            AutoRegisterTemplate = true,
            AutoRegisterTemplateVersion = AutoRegisterTemplateVersion.ESv7,
            NumberOfShards = 1,
            NumberOfReplicas = 0,
            MinimumLogEventLevel = Serilog.Events.LogEventLevel.Information
        });
    }
});

builder.Services.AddControllers();
builder.Services.AddHttpContextAccessor();

var redisConnectionString = Environment.GetEnvironmentVariable("REDIS_CONNECTION_STRING")
                            ?? builder.Configuration["Redis:ConnectionString"]
                            ?? "localhost:6379";

var signalRBackplaneEnabled = bool.TryParse(
    Environment.GetEnvironmentVariable("SIGNALR_REDIS_BACKPLANE_ENABLED"),
    out var parsedSignalRBackplaneEnabled)
    ? parsedSignalRBackplaneEnabled
    : builder.Configuration.GetValue("SignalR:RedisBackplaneEnabled", false);

var signalRChannelPrefix = Environment.GetEnvironmentVariable("SIGNALR_CHANNEL_PREFIX")
                           ?? builder.Configuration["SignalR:ChannelPrefix"]
                           ?? "ecommerce";

var signalRBuilder = builder.Services.AddSignalR(options =>
{
    options.EnableDetailedErrors = builder.Environment.IsDevelopment();
});

if (signalRBackplaneEnabled)
{
    signalRBuilder.AddStackExchangeRedis(redisConnectionString, options =>
    {
        options.Configuration.ChannelPrefix = RedisChannel.Literal($"{signalRChannelPrefix}:signalr");
        options.Configuration.AbortOnConnectFail = false;
        options.Configuration.ConnectRetry = 3;
    });
}

var dataProtectionKeysPath = builder.Configuration["DataProtection:KeysPath"];

if (string.IsNullOrWhiteSpace(dataProtectionKeysPath))
{
    dataProtectionKeysPath = Path.Combine(builder.Environment.ContentRootPath, "data-protection-keys");
}

Directory.CreateDirectory(dataProtectionKeysPath);

builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(dataProtectionKeysPath))
    .SetApplicationName("EcommerceAPI");


var redisMultiplexer = ConnectionMultiplexer.Connect(redisConnectionString);
builder.Services.AddSingleton<IConnectionMultiplexer>(redisMultiplexer);

var allowedOrigins = ResolveAllowedOrigins(
    builder.Environment.EnvironmentName,
    Environment.GetEnvironmentVariable("ALLOWED_ORIGINS"),
    builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>());

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(allowedOrigins)
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
});

builder.Services.AddHsts(options =>
{
    options.Preload = true;
    options.IncludeSubDomains = true;
    options.MaxAge = TimeSpan.FromDays(180);
});

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
var rabbitMqHost = Environment.GetEnvironmentVariable("RABBITMQ_HOST")
                   ?? builder.Configuration["RabbitMQ:Host"]
                   ?? "localhost";
var rabbitMqVirtualHost = Environment.GetEnvironmentVariable("RABBITMQ_VHOST")
                          ?? builder.Configuration["RabbitMQ:VirtualHost"]
                          ?? "/";
var rabbitMqUsername = Environment.GetEnvironmentVariable("RABBITMQ_USER")
                       ?? builder.Configuration["RabbitMQ:Username"]
                       ?? "guest";
var rabbitMqPassword = Environment.GetEnvironmentVariable("RABBITMQ_PASSWORD")
                       ?? builder.Configuration["RabbitMQ:Password"]
                       ?? "guest";
var rabbitMqPort = builder.Configuration.GetValue("RabbitMQ:Port", 5672);
var openTelemetryEnabled = builder.Configuration.GetValue("OpenTelemetry:Enabled", !builder.Environment.IsEnvironment("Test"));
var openTelemetryServiceName = builder.Configuration["OpenTelemetry:ServiceName"] ?? "EcommerceAPI.API";
var openTelemetryOtlpEndpoint = builder.Configuration["OpenTelemetry:OtlpEndpoint"]
                                ?? Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT");
var openTelemetryServiceVersion = typeof(Program).Assembly.GetName().Version?.ToString() ?? "1.0.0";

var rabbitMqPrefetchCount = builder.Configuration.GetValue("RabbitMQ:PrefetchCount", 32);
var rabbitMqPrefetchCountFromEnv = Environment.GetEnvironmentVariable("RABBITMQ_PREFETCH_COUNT");

if (int.TryParse(rabbitMqPrefetchCountFromEnv, out var parsedPrefetchCount))
{
    rabbitMqPrefetchCount = parsedPrefetchCount;
}

var rabbitMqPortFromEnv = Environment.GetEnvironmentVariable("RABBITMQ_PORT");
if (int.TryParse(rabbitMqPortFromEnv, out var parsedRabbitMqPort))
{
    rabbitMqPort = parsedRabbitMqPort;
}

builder.Services.AddBusinessServices(connectionString!);
builder.Services.AddInfrastructureServices(builder.Configuration, builder.Environment.EnvironmentName);

if (openTelemetryEnabled)
{
    var openTelemetryBuilder = builder.Services.AddOpenTelemetry()
        .ConfigureResource(resource =>
        {
            resource.AddService(
                serviceName: openTelemetryServiceName,
                serviceVersion: openTelemetryServiceVersion);
            resource.AddAttributes(new[]
            {
                new KeyValuePair<string, object>("deployment.environment", builder.Environment.EnvironmentName)
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
                options.SetDbStatementForText = builder.Environment.IsDevelopment();
            })
            .AddRedisInstrumentation(options =>
            {
                options.SetVerboseDatabaseStatements = builder.Environment.IsDevelopment();
            })
            .AddSource("MassTransit");

        if (!string.IsNullOrWhiteSpace(openTelemetryOtlpEndpoint))
        {
            tracing.AddOtlpExporter(options =>
            {
                options.Endpoint = new Uri(openTelemetryOtlpEndpoint);
            });
        }
    });

    openTelemetryBuilder.WithMetrics(metrics =>
    {
        metrics
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddRuntimeInstrumentation();

        if (!string.IsNullOrWhiteSpace(openTelemetryOtlpEndpoint))
        {
            metrics.AddOtlpExporter(options =>
            {
                options.Endpoint = new Uri(openTelemetryOtlpEndpoint);
            });
        }
    });
}

builder.Services.AddMassTransit(configurator =>
{
    configurator.SetKebabCaseEndpointNameFormatter();
    configurator.AddEntityFrameworkOutbox<AppDbContext>(options =>
    {
        options.UsePostgres();
        options.UseBusOutbox();
    });
    configurator.AddConsumer<OrderCreatedConsumer, OrderCreatedConsumerDefinition>();
    configurator.AddConsumer<ProductIndexSyncConsumer, ProductIndexSyncConsumerDefinition>();
    configurator.AddConsumer<WishlistAnalyticsConsumer, WishlistAnalyticsConsumerDefinition>();
    configurator.AddConsumer<WishlistProductIndexSyncConsumer, WishlistProductIndexSyncConsumerDefinition>();
    configurator.AddConsumer<WishlistPersonalizationConsumer, WishlistPersonalizationConsumerDefinition>();
    configurator.AddConsumer<WishlistPriceAlertNotificationConsumer, WishlistPriceAlertNotificationConsumerDefinition>();
    configurator.AddConsumer<WishlistLowStockNotificationConsumer, WishlistLowStockNotificationConsumerDefinition>();
    configurator.AddConsumer<CampaignStatusChangedConsumer, CampaignStatusChangedConsumerDefinition>();
    if (!builder.Environment.IsEnvironment("Test"))
    {
        configurator.AddConsumer<RefundRequestedConsumer, RefundRequestedConsumerDefinition>();
    }

    if (builder.Environment.IsEnvironment("Test"))
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

builder.Services.AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy(), tags: new[] { "live" })
    .AddCheck<DatabaseHealthCheck>("postgresql", tags: new[] { "ready" })
    .AddCheck<RedisHealthCheck>("redis", tags: new[] { "ready" });

var rateLimitingEnabled = builder.Configuration.GetValue("RateLimiting:Enabled", true);
if (rateLimitingEnabled)
{
    builder.Services.AddRateLimiter(options =>
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
}

var hangfireEnabled = builder.Configuration.GetValue("Hangfire:Enabled", !builder.Environment.IsEnvironment("Test"));
if (hangfireEnabled)
{
    builder.Services.AddHangfire(config => config
        .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
        .UseSimpleAssemblyNameTypeSerializer()
        .UseRecommendedSerializerSettings()
        .UsePostgreSqlStorage(options =>
            options.UseNpgsqlConnection(connectionString)));

    builder.Services.AddHangfireServer();
}

var jwtSecretKey = builder.Configuration["JWT_SECRET_KEY"]
    ?? throw new InvalidOperationException("JWT_SECRET_KEY environment variable is required. Application cannot start without it.");
var jwtIssuer = builder.Configuration["JWT_ISSUER"]
    ?? throw new InvalidOperationException("JWT_ISSUER environment variable is required. Application cannot start without it.");
var jwtAudience = builder.Configuration["JWT_AUDIENCE"]
    ?? throw new InvalidOperationException("JWT_AUDIENCE environment variable is required. Application cannot start without it.");

if (Encoding.UTF8.GetByteCount(jwtSecretKey) < 32)
{
    throw new InvalidOperationException("JWT_SECRET_KEY en az 32 byte olmalıdır.");
}

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = !builder.Environment.IsDevelopment() && !builder.Environment.IsEnvironment("Test");
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

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
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

var app = builder.Build();

ServiceTool.SetProvider(app.Services);

if (!app.Environment.IsEnvironment("Test"))
{
    var startupLogger = app.Services.GetRequiredService<ILogger<Program>>();
    startupLogger.LogInformation(
        "RabbitMQ bus configured for host {Host}:{Port} (vhost: {VirtualHost})",
        rabbitMqHost,
        rabbitMqPort,
        rabbitMqVirtualHost);
}

using (var scope = app.Services.CreateScope())
{
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
                builder.Environment.ContentRootPath,
                AppContext.BaseDirectory,
                Directory.GetCurrentDirectory());
            
            logger.LogInformation("📁 Seed data path: {SeedPath}, Exists: {Exists}", seedPath, Directory.Exists(seedPath));

            var seeder = new SeedRunner(context, logger, seedPath, hashingService, encryptionService);
            await seeder.RunAsync(seed: true);
        }
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while migrating or seeding the database");
    }
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else if (!app.Environment.IsEnvironment("Test"))
{
    app.UseHsts();
}

if (hangfireEnabled)
{
    app.UseHangfireDashboard("/hangfire", new DashboardOptions
    {
        Authorization = new[] { new HangfireAuthorizationFilter() }
    });
}

if (hangfireEnabled)
{
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
}

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

// API yanıtları için temel güvenlik başlıkları.
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

if (app.Configuration.GetValue("RateLimiting:Enabled", true))
{
    app.UseRateLimiter();
}

app.UseAuthentication();
app.UseAuthorization();

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

static string ResolveSeedDataPath(params string[] basePaths)
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

static string[] ResolveAllowedOrigins(string environmentName, string? envOrigins, string[]? configuredOrigins)
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

static bool IsElasticsearchRequest(Uri? uri)
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

static void ConfigureRedisRateLimiterPolicies(Microsoft.AspNetCore.RateLimiting.RateLimiterOptions options, IConnectionMultiplexer redisMultiplexer)
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

static void AddRedisFixedWindowLimiter(
    Microsoft.AspNetCore.RateLimiting.RateLimiterOptions options,
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

static void AddRedisSlidingWindowLimiter(
    Microsoft.AspNetCore.RateLimiting.RateLimiterOptions options,
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

app.Run();

public partial class Program { }
