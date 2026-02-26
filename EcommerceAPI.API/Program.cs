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
builder.Services.AddSignalR();
builder.Services.AddHttpContextAccessor();

var dataProtectionKeysPath = builder.Configuration["DataProtection:KeysPath"];

if (string.IsNullOrWhiteSpace(dataProtectionKeysPath))
{
    dataProtectionKeysPath = Path.Combine(builder.Environment.ContentRootPath, "data-protection-keys");
}

Directory.CreateDirectory(dataProtectionKeysPath);

builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(dataProtectionKeysPath))
    .SetApplicationName("EcommerceAPI");


builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
    ConnectionMultiplexer.Connect(builder.Configuration["Redis:ConnectionString"] ?? "localhost:6379"));

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(
                "http://localhost:5173",
                "http://localhost:3000",
                "http://localhost:80",
                "http://ecommerce.berkansozer.com",
                "https://ecommerce.berkansozer.com"
            )
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
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
    configurator.AddConsumer<OrderCreatedConsumer, OrderCreatedConsumerDefinition>();
    configurator.AddConsumer<ProductIndexSyncConsumer, ProductIndexSyncConsumerDefinition>();

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

if (!builder.Environment.IsEnvironment("Test"))
{
    builder.Services.AddHostedService<OutboxPublisherBackgroundService>();
}


var rateLimitingEnabled = builder.Configuration.GetValue("RateLimiting:Enabled", true);
if (rateLimitingEnabled)
{
    builder.Services.AddRateLimiter(options =>
    {
        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
        
        options.OnRejected = async (context, token) =>
        {
            if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
            {
                context.HttpContext.Response.Headers.RetryAfter = 
                    ((int)retryAfter.TotalSeconds).ToString();
            }
            
            context.HttpContext.Response.ContentType = "application/json";
            await context.HttpContext.Response.WriteAsJsonAsync(new
            {
                errorCode = "RATE_LIMIT_EXCEEDED",
                message = "Çok fazla istek gönderdiniz. Lütfen bekleyin.",
                retryAfterSeconds = retryAfter.TotalSeconds
            }, token);
        };

        options.AddRedisFixedWindowLimiter("auth", (opt) =>
        {
            opt.ConnectionMultiplexerFactory = () => ConnectionMultiplexer.Connect(
                Environment.GetEnvironmentVariable("REDIS_CONNECTION_STRING") 
                ?? builder.Configuration["Redis:ConnectionString"] 
                ?? "localhost:6379");
            opt.PermitLimit = 5;
            opt.Window = TimeSpan.FromMinutes(1);
        });

        options.AddRedisSlidingWindowLimiter("payment", (opt) =>
        {
            opt.ConnectionMultiplexerFactory = () => ConnectionMultiplexer.Connect(
                Environment.GetEnvironmentVariable("REDIS_CONNECTION_STRING") 
                ?? builder.Configuration["Redis:ConnectionString"] 
                ?? "localhost:6379");
            opt.PermitLimit = 10;
            opt.Window = TimeSpan.FromMinutes(1);
        });
        
        options.AddRedisFixedWindowLimiter("global", (opt) =>
        {
            opt.ConnectionMultiplexerFactory = () => ConnectionMultiplexer.Connect(
                Environment.GetEnvironmentVariable("REDIS_CONNECTION_STRING") 
                ?? builder.Configuration["Redis:ConnectionString"] 
                ?? "localhost:6379");
            opt.PermitLimit = 100;
            opt.Window = TimeSpan.FromMinutes(1);
        });

        options.AddRedisFixedWindowLimiter("search", opt =>
        {
            opt.ConnectionMultiplexerFactory = () => ConnectionMultiplexer.Connect(
                Environment.GetEnvironmentVariable("REDIS_CONNECTION_STRING")
                ?? builder.Configuration["Redis:ConnectionString"]
                ?? "localhost:6379");
            opt.PermitLimit = 60; // 1 dakikada 60 arama
            opt.Window = TimeSpan.FromMinutes(1);
        });

        options.AddRedisFixedWindowLimiter("support-message-http", opt =>
        {
            opt.ConnectionMultiplexerFactory = () => ConnectionMultiplexer.Connect(
                Environment.GetEnvironmentVariable("REDIS_CONNECTION_STRING")
                ?? builder.Configuration["Redis:ConnectionString"]
                ?? "localhost:6379");
            opt.PermitLimit = 20; // 1 dakikada 20 mesaj (HTTP fallback)
            opt.Window = TimeSpan.FromMinutes(1);
        });

        options.AddRedisFixedWindowLimiter("support-hub-connect", opt =>
        {
            opt.ConnectionMultiplexerFactory = () => ConnectionMultiplexer.Connect(
                Environment.GetEnvironmentVariable("REDIS_CONNECTION_STRING")
                ?? builder.Configuration["Redis:ConnectionString"]
                ?? "localhost:6379");
            opt.PermitLimit = 30; // 1 dakikada 30 hub connect
            opt.Window = TimeSpan.FromMinutes(1);
        });

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
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["JWT_ISSUER"],
        ValidAudience = builder.Configuration["JWT_AUDIENCE"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecretKey))
    };

    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];
            var path = context.HttpContext.Request.Path;

            if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs/live-support"))
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

app.Run();

public partial class Program { }
