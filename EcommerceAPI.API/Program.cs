using EcommerceAPI.Core.Interfaces;
using EcommerceAPI.DataAccess.Abstract;
using EcommerceAPI.DataAccess.Concrete.EntityFramework;
using EcommerceAPI.DataAccess.Concrete.EntityFramework.Contexts;
using Microsoft.EntityFrameworkCore;
using EcommerceAPI.Business.Abstract;
using EcommerceAPI.Business.Concrete;
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
using EcommerceAPI.DataAccess;
using FluentValidation;
using FluentValidation.AspNetCore;
using EcommerceAPI.Business.Validators;

var builder = WebApplication.CreateBuilder(args);

// ---- Serilog ----
builder.Host.UseSerilog((context, configuration) =>
    configuration.ReadFrom.Configuration(context.Configuration));
// ---- Serilog ----

builder.Services.AddControllers();

// ---- FluentValidation ----
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<CreateProductRequestValidator>();
// ---- FluentValidation ----

// ---- CORS ----
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(
                "http://localhost:5173",  // Vite dev server
                "http://localhost:3000",  // Docker dev
                "http://localhost:80"     // Docker prod
            )
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
});
// ---- CORS ----

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

// Data Access Layer (DAL) Registrations
builder.Services.AddScoped<IProductDal, EfProductDal>();
builder.Services.AddScoped<IOrderDal, EfOrderDal>();
builder.Services.AddScoped<ICategoryDal, EfCategoryDal>();
builder.Services.AddScoped<ICartDal, EfCartDal>();
builder.Services.AddScoped<IInventoryDal, EfInventoryDal>();
builder.Services.AddScoped<IUserDal, EfUserDal>();
builder.Services.AddScoped<IRoleDal, EfRoleDal>();
builder.Services.AddScoped<IRefreshTokenDal, EfRefreshTokenDal>();
builder.Services.AddScoped<IPaymentDal, EfPaymentDal>();
builder.Services.AddScoped<IShippingAddressDal, EfShippingAddressDal>();
builder.Services.AddScoped<ICouponDal, EfCouponDal>();

builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// Business Layer Registrations
builder.Services.AddScoped<IProductService, ProductManager>();
builder.Services.AddScoped<IOrderService, OrderManager>();
builder.Services.AddScoped<ICategoryService, CategoryManager>();
builder.Services.AddScoped<ICartService, CartManager>();
builder.Services.AddScoped<IInventoryService, InventoryManager>();
builder.Services.AddScoped<IAuthService, AuthManager>();
builder.Services.AddScoped<IPaymentService, IyzicoPaymentManager>();
builder.Services.AddScoped<IShippingAddressService, ShippingAddressManager>();
builder.Services.AddScoped<ICouponService, CouponManager>();

builder.Services.AddScoped<ICartMapper, CartMapper>();

// ---- KVKK Encryption Services ----
builder.Services.AddSingleton<IEncryptionService, EncryptionService>();
builder.Services.AddSingleton<IHashingService, HashingService>();
// ---- KVKK Encryption Services ----

// ---- Redis Cache ----
var redisEnv = Environment.GetEnvironmentVariable("REDIS_CONNECTION_STRING");
var redisConfig = builder.Configuration["Redis:ConnectionString"];
var redisConnection = "localhost:6379";

if (!string.IsNullOrWhiteSpace(redisEnv))
{
    redisConnection = redisEnv;
}
else if (!string.IsNullOrWhiteSpace(redisConfig))
{
    redisConnection = redisConfig;
}
if (builder.Environment.IsEnvironment("Test"))
{
    redisConnection = "localhost:6379";
}

builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = redisConnection;
    options.InstanceName = "EcommerceAPI_";
});
builder.Services.AddScoped<ICacheService, RedisCacheService>();

// ---- Redis Rate Limiting ----
var redis = ConnectionMultiplexer.Connect(redisConnection);
builder.Services.AddSingleton<IConnectionMultiplexer>(redis);

// Rate Limiting - Can be disabled via RateLimiting:Enabled = false (for integration tests)
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
}
// ---- Redis Rate Limiting ----

// ---- Reuse Redis Cache section ----

// ---- Hangfire ----
// Hangfire sadece non-Test ortamında çalışır (Integration testleri için devre dışı)
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
        var hashingService = services.GetRequiredService<IHashingService>();
        var logger = services.GetRequiredService<ILogger<DbInitializer>>();
        var initializer = new DbInitializer(context, hashingService, logger);
        await initializer.InitializeAsync();
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
if (!app.Environment.IsEnvironment("Test"))
{
    RecurringJob.AddOrUpdate<IOrderService>(
        "cancel-expired-orders",
        service => service.CancelExpiredOrdersAsync(),
        "*/15 * * * *"); // Her 15 dakikada bir çalışır (cron expression)
}
// ---- Hangfire tekrarlayan işler ----

app.UseCorrelationId();
app.UseSerilogRequestLogging();
app.UseExceptionHandling();

app.UseCors("AllowFrontend");

app.UseHttpsRedirection();

// Rate limiter - only if enabled (disabled in Test environment)
if (app.Configuration.GetValue("RateLimiting:Enabled", true))
{
    app.UseRateLimiter();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

public partial class Program { }
