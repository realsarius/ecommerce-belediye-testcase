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
var rabbitMqPortFromEnv = Environment.GetEnvironmentVariable("RABBITMQ_PORT");
if (int.TryParse(rabbitMqPortFromEnv, out var parsedRabbitMqPort))
{
    rabbitMqPort = parsedRabbitMqPort;
}

builder.Services.AddBusinessServices(connectionString!);
builder.Services.AddInfrastructureServices(builder.Configuration, builder.Environment.EnvironmentName);

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

        cfg.ConfigureEndpoints(context);
    });
});

builder.Services.AddHealthChecks();

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

if (!builder.Environment.IsEnvironment("Test"))
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
            
            var possiblePaths = new[]
            {
                Path.Combine(AppContext.BaseDirectory, "seed-data"),
                Path.Combine(Directory.GetCurrentDirectory(), "..", "seed-data"),
                Path.Combine(Directory.GetCurrentDirectory(), "seed-data")
            };
            
            var seedPath = possiblePaths.FirstOrDefault(Directory.Exists) 
                ?? Path.Combine(AppContext.BaseDirectory, "seed-data");
            
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

if (!app.Environment.IsEnvironment("Test"))
{
    app.UseHangfireDashboard("/hangfire", new DashboardOptions
    {
        Authorization = new[] { new HangfireAuthorizationFilter() }
    });
}

if (!app.Environment.IsEnvironment("Test"))
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
app.UseSerilogRequestLogging();
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
app.MapHealthChecks("/health");
app.MapHub<LiveSupportHub>("/hubs/live-support")
    .RequireRateLimiting("support-hub-connect");

app.Run();

public partial class Program { }
