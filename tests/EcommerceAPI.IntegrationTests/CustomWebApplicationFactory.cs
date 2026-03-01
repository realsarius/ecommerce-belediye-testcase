using System.Linq;
using System.Threading.Channels;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using EcommerceAPI.DataAccess.Concrete.EntityFramework.Contexts;
using EcommerceAPI.API;
using EcommerceAPI.Entities.Concrete;
using EcommerceAPI.Entities.Enums;
using EcommerceAPI.IntegrationTests.Utilities;

namespace EcommerceAPI.IntegrationTests;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    public CustomWebApplicationFactory()
    {
        Environment.SetEnvironmentVariable("JWT_SECRET_KEY", "all-good-things-to-those-who-wait-i-have-waited-clarice-but-how-long-can-you-and-old-jackie-boy-wait");
        Environment.SetEnvironmentVariable("JWT_ISSUER", "EcommerceAPI");
        Environment.SetEnvironmentVariable("JWT_AUDIENCE", "EcommerceAPI");
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting(WebHostDefaults.EnvironmentKey, "Test");
        builder.UseEnvironment("Test");

        builder.ConfigureAppConfiguration((ctx, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "Redis:ConnectionString", "localhost:6379" },
                { "JWT_SECRET_KEY", "all-good-things-to-those-who-wait-i-have-waited-clarice-but-how-long-can-you-and-old-jackie-boy-wait" },
                { "JWT_ISSUER", "EcommerceAPI" },
                { "JWT_AUDIENCE", "EcommerceAPI" },
                { "Cors:AllowedOrigins:0", "http://localhost:5173" },
                { "Cors:AllowedOrigins:1", "http://localhost:3000" },
                { "Cors:AllowedOrigins:2", "http://localhost:80" },
                { "Iyzico:ApiKey", "sandbox-test" },
                { "Iyzico:SecretKey", "sandbox-test" },
                { "ENCRYPTION_KEY", "12345678901234567890123456789012" },
                { "HASH_PEPPER", "test-pepper-value" },
                { "RateLimiting:Enabled", "false" },
                { "ConnectionStrings:DefaultConnection", "Host=localhost;Port=5433;Database=ecommerce_test;Username=ecommerce_test_user;Password=test_password" }
            });
        });

        builder.ConfigureServices(services =>
        {
            var dbContextDescriptor = services.SingleOrDefault(d =>
                d.ServiceType == typeof(DbContextOptions<AppDbContext>));

            if (dbContextDescriptor != null)
                services.Remove(dbContextDescriptor);

            var sp = services.BuildServiceProvider();
            var config = sp.GetRequiredService<IConfiguration>();

            var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
                     ?? config.GetConnectionString("DefaultConnection") 
                     ?? "Host=localhost;Port=5433;Database=ecommerce_test;Username=ecommerce_test_user;Password=test_password";

            services.AddDbContext<AppDbContext>(opt =>
                opt.UseNpgsql(connectionString));

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = TestAuthHandler.AuthenticationScheme;
                options.DefaultChallengeScheme = TestAuthHandler.AuthenticationScheme;
            })
            .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
                TestAuthHandler.AuthenticationScheme, _ => { });
        });
    }

    protected override void Dispose(bool disposing)
    {
        try
        {
            base.Dispose(disposing);
        }
        catch (ChannelClosedException)
        {
            // CI occasionally tears down the MassTransit in-memory transport twice.
            // Once the channel is already closed, we can safely ignore the duplicate stop.
        }
    }
}

public static class TestDataSeeder
{
    public static async Task<User> EnsureUserAsync(AppDbContext db, int userId, string role = "Customer")
    {
        var user = await db.Users.FindAsync(userId);
        if (user != null) return user;

        var existingRole = await db.Roles.FirstOrDefaultAsync(r => r.Name == role);
        if (existingRole == null)
        {
            existingRole = new Role { Name = role, Description = $"{role} role" };
            db.Roles.Add(existingRole);
            await db.SaveChangesAsync();
        }

        user = new User
        {
            Id = userId,
            Email = $"testuser{userId}@test.com",
            EmailHash = $"hash_{userId}",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("TestPassword123!"),
            FirstName = "Test",
            LastName = $"User{userId}",
            RoleId = existingRole.Id
        };
        
        db.Users.Add(user);
        await db.SaveChangesAsync();
        
        return user;
    }

    public static async Task<Category> EnsureCategoryAsync(AppDbContext db, int categoryId = 1, string? name = null)
    {
        var category = await db.Categories.FindAsync(categoryId);
        if (category != null) return category;

        var categoryName = string.IsNullOrWhiteSpace(name)
            ? $"Test Category {categoryId}"
            : name;

        var existingByName = await db.Categories.FirstOrDefaultAsync(c => c.Name == categoryName);
        if (existingByName != null)
        {
            return existingByName;
        }

        category = new Category
        {
            Id = categoryId,
            Name = categoryName,
            Description = "Category for integration tests",
            IsActive = true
        };
        
        db.Categories.Add(category);
        await db.SaveChangesAsync();
        
        return category;
    }

    public static async Task<Product> EnsureProductWithStockAsync(
        AppDbContext db, 
        int productId = 1, 
        int categoryId = 1,
        int stockQuantity = 100)
    {
        await EnsureCategoryAsync(db, categoryId);
        
        var product = await db.Products
            .Include(p => p.Inventory)
            .FirstOrDefaultAsync(p => p.Id == productId);
            
        if (product != null) return product;

        product = new Product
        {
            Id = productId,
            Name = $"Test Product {productId}",
            Description = "Product for integration tests",
            Price = 99.99m,
            SKU = $"TEST-{productId:D4}",
            CategoryId = categoryId,
            IsActive = true
        };
        
        db.Products.Add(product);
        await db.SaveChangesAsync();

        var inventory = new Inventory
        {
            ProductId = productId,
            QuantityAvailable = stockQuantity,
            QuantityReserved = 0
        };
        
        db.Set<Inventory>().Add(inventory);
        await db.SaveChangesAsync();
        
        return product;
    }

    public static async Task<Order> EnsureOrderWithPaymentAsync(
        AppDbContext db,
        int orderId,
        int userId,
        int productId,
        int categoryId,
        string orderNumber,
        string paymentIdempotencyKey,
        OrderStatus orderStatus = OrderStatus.Paid,
        PaymentStatus paymentStatus = PaymentStatus.Success)
    {
        await EnsureUserAsync(db, userId);
        var product = await EnsureProductWithStockAsync(db, productId, categoryId, 25);

        var existingOrder = await db.Orders
            .Include(o => o.Payment)
            .Include(o => o.OrderItems)
            .FirstOrDefaultAsync(o => o.Id == orderId);

        if (existingOrder != null)
        {
            return existingOrder;
        }

        var order = new Order
        {
            Id = orderId,
            UserId = userId,
            OrderNumber = orderNumber,
            Status = orderStatus,
            TotalAmount = product.Price,
            ShippingAddress = "Integration Test Address 123",
            Notes = "Integration seeded order"
        };

        order.OrderItems.Add(new OrderItem
        {
            ProductId = product.Id,
            Quantity = 1,
            PriceSnapshot = product.Price
        });

        order.Payment = new Payment
        {
            Amount = product.Price,
            Currency = "TRY",
            Status = paymentStatus,
            PaymentMethod = "CreditCard",
            PaymentProviderId = paymentStatus == PaymentStatus.Success ? $"PAY-{orderNumber}" : null,
            IdempotencyKey = paymentIdempotencyKey
        };

        db.Orders.Add(order);
        await db.SaveChangesAsync();

        return order;
    }
}
