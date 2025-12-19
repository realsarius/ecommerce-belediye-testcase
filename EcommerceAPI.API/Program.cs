using EcommerceAPI.Core.Interfaces;
using EcommerceAPI.Data;
using EcommerceAPI.Data.Repositories;
using Microsoft.EntityFrameworkCore;
using EcommerceAPI.Business.Services;
using EcommerceAPI.Business.Mappers;
using EcommerceAPI.Business.Settings;
using EcommerceAPI.API.Middleware;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Serilog;

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
// ---- Redis Cache ----

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
}

app.UseCorrelationId();
app.UseSerilogRequestLogging();
app.UseExceptionHandling();

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

