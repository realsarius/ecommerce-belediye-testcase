using EcommerceAPI.Business;
using EcommerceAPI.Application.Abstractions.ServiceContracts;
using EcommerceAPI.Infrastructure;
using EcommerceAPI.DataAccess.Concrete.EntityFramework.Contexts;
using EcommerceAPI.DataAccess;
using EcommerceAPI.Core.Interfaces;
using Microsoft.EntityFrameworkCore;
using EcommerceAPI.Seeder;
using EcommerceAPI.API.Middleware;
using EcommerceAPI.API.Hubs;
using EcommerceAPI.API.Extensions;
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
using EcommerceAPI.API.Services;
using EcommerceAPI.API.Authorization;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.DataProtection;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using OpenTelemetry.Metrics;
using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using System.Text.Json;

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
builder.Services.AddHostedService<OutboxPublisherBackgroundService>();

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


var redisConfiguration = ConfigurationOptions.Parse(redisConnectionString);
redisConfiguration.AbortOnConnectFail = false;
redisConfiguration.ConnectRetry = 3;
redisConfiguration.ReconnectRetryPolicy = new ExponentialRetry(5_000);

var redisMultiplexer = ConnectionMultiplexer.Connect(redisConfiguration);
builder.Services.AddSingleton<IConnectionMultiplexer>(redisMultiplexer);

var allowedOrigins = WebApplicationExtensions.ResolveAllowedOrigins(
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

builder.Services.AddDataAccessServices(connectionString!);
builder.Services.AddBusinessServices();
builder.Services.AddInfrastructureServices(builder.Configuration, builder.Environment.EnvironmentName);

builder.Services.AddConfiguredOpenTelemetry(
    enabled: openTelemetryEnabled,
    serviceName: openTelemetryServiceName,
    serviceVersion: openTelemetryServiceVersion,
    environmentName: builder.Environment.EnvironmentName,
    otlpEndpoint: openTelemetryOtlpEndpoint,
    isDevelopment: builder.Environment.IsDevelopment());

builder.Services.AddConfiguredMassTransit(
    isTestEnvironment: builder.Environment.IsEnvironment("Test"),
    rabbitMqHost: rabbitMqHost,
    rabbitMqPort: rabbitMqPort,
    rabbitMqVirtualHost: rabbitMqVirtualHost,
    rabbitMqUsername: rabbitMqUsername,
    rabbitMqPassword: rabbitMqPassword,
    rabbitMqPrefetchCount: rabbitMqPrefetchCount);

builder.Services.AddConfiguredHealthChecks();

var rateLimitingEnabled = builder.Configuration.GetValue("RateLimiting:Enabled", !builder.Environment.IsEnvironment("Test"));
builder.Services.AddConfiguredRateLimiting(
    enabled: rateLimitingEnabled,
    redisMultiplexer: redisMultiplexer);

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

builder.Services.AddConfiguredAuthenticationAndAuthorization(
    configuration: builder.Configuration,
    environment: builder.Environment);

builder.Services.AddConfiguredSwaggerDocumentation();

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

await app.ApplyDataInitializationAsync(builder.Configuration);

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

app.ConfigureRecurringJobs(hangfireEnabled);

app.UseApiRequestPipeline(app.Configuration.GetValue("RateLimiting:Enabled", true));
app.MapApiEndpoints();

app.Run();

public partial class Program { }
