using EcommerceAPI.Core.Interfaces;
using EcommerceAPI.Data;
using EcommerceAPI.Data.Repositories;
using Microsoft.EntityFrameworkCore;
using EcommerceAPI.Business.Services.Abstract;
using EcommerceAPI.Business.Services.Concrete;
using EcommerceAPI.Business.Mappers;
using EcommerceAPI.Business.Settings;
using EcommerceAPI.API.Middleware;
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

var builder = WebApplication.CreateBuilder(args);

// ---- Serilog ----
builder.Host.UseSerilog((context, configuration) =>
    configuration.ReadFrom.Configuration(context.Configuration));
// ---- Serilog ----

builder.Services.AddControllers();

// ---- Iyzico Settings ----
builder.Services.Configure<IyzicoSettings>(options =>
{
    var config = builder.Configuration.GetSection("Iyzico");
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
// ---- Iyzico Settings ----

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddScoped(typeof(IRepository<>), typeof(GenericRepository<>));
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<IInventoryRepository, InventoryRepository>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<ICartRepository, CartRepository>();
builder.Services.AddScoped<ICartService, CartService>();
builder.Services.AddScoped<ICartMapper, CartMapper>();
builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<IInventoryService, InventoryService>();
builder.Services.AddScoped<IPaymentService, IyzicoPaymentService>();

// ---- KVKK Encryption Services ----
builder.Services.AddSingleton<IEncryptionService, EncryptionService>();
builder.Services.AddSingleton<IHashingService, HashingService>();
// ---- KVKK Encryption Services ----

// ---- Redis Cache ----
var redisConnection = Environment.GetEnvironmentVariable("REDIS_CONNECTION_STRING") 
                      ?? builder.Configuration["Redis:ConnectionString"]
                      ?? throw new InvalidOperationException("Redis connection string is not configured. Set REDIS_CONNECTION_STRING environment variable or Redis:ConnectionString in appsettings.");
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = redisConnection;
    options.InstanceName = "EcommerceAPI_";
});
builder.Services.AddScoped<ICacheService, RedisCacheService>();

// ---- Redis Rate Limiting ----
var redis = ConnectionMultiplexer.Connect(redisConnection);
builder.Services.AddSingleton<IConnectionMultiplexer>(redis);

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

    // Auth Policy (IP-based): 5 requests/minute - Brute-force koruması
    options.AddRedisFixedWindowLimiter("auth", (opt) =>
    {
        opt.ConnectionMultiplexerFactory = () => redis;
        opt.PermitLimit = 5;
        opt.Window = TimeSpan.FromMinutes(1);
    });

    // Payment Policy (User-based): 10 requests/minute - Payment abuse koruması
    options.AddRedisSlidingWindowLimiter("payment", (opt) =>
    {
        opt.ConnectionMultiplexerFactory = () => redis;
        opt.PermitLimit = 10;
        opt.Window = TimeSpan.FromMinutes(1);
    });
    
    // Global Policy (IP-based): 100 requests/minute - DDoS koruması
    options.AddRedisFixedWindowLimiter("global", (opt) =>
    {
        opt.ConnectionMultiplexerFactory = () => redis;
        opt.PermitLimit = 100;
        opt.Window = TimeSpan.FromMinutes(1);
    });
});
// ---- Redis Rate Limiting ----

// ---- Redis Cache ----

// ---- Hangfire ----
builder.Services.AddHangfire(config => config
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UsePostgreSqlStorage(options =>
        options.UseNpgsqlConnection(connectionString)));

builder.Services.AddHangfireServer();
// ---- Hangfire ----

// ---- JWT Authentication ----
var jwtSecretKey = builder.Configuration["JWT_SECRET_KEY"] ?? "default-development-key-min-32-chars";
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
});
// ---- JWT Authentication ----

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


using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<AppDbContext>();
        var logger = services.GetRequiredService<ILogger<Program>>();
        await DbInitializer.InitializeAsync(context, logger);
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
    app.UseHangfireDashboard("/hangfire", new DashboardOptions
    {
        Authorization = new[] { new HangfireAuthorizationFilter() }
    });
}

// ---- Hangfire tekrarlayan işler ----
RecurringJob.AddOrUpdate<IOrderService>(
    "cancel-expired-orders",
    service => service.CancelExpiredOrdersAsync(),
    "*/15 * * * *"); // Her 15 dakikada bir çalışır (cron expression)
// ---- Hangfire tekrarlayan işler ----

app.UseCorrelationId();
app.UseSerilogRequestLogging();
app.UseExceptionHandling();

app.UseHttpsRedirection();

app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

