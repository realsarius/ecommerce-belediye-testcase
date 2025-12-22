using System.Linq;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using EcommerceAPI.Data;
using EcommerceAPI.API;
using EcommerceAPI.Core.Entities;
using EcommerceAPI.IntegrationTests.Utilities;

namespace EcommerceAPI.IntegrationTests;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // En kuvvetli override - Environment kesin "Test" olmalı
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
                { "Iyzico:ApiKey", "sandbox-test" },
                { "Iyzico:SecretKey", "sandbox-test" },
                { "ENCRYPTION_KEY", "12345678901234567890123456789012" }, // 32-char key for AES-256
                { "HASH_PEPPER", "test-pepper-value" },
                { "RateLimiting:Enabled", "false" }, // Disable rate limiting in tests
                { "ConnectionStrings:DefaultConnection", "Host=localhost;Port=5433;Database=ecommerce_test;Username=ecommerce_test_user;Password=test_password" }
            });
        });

        builder.ConfigureServices(services =>
        {
            // Replace DbContext with test database
            var dbContextDescriptor = services.SingleOrDefault(d =>
                d.ServiceType == typeof(DbContextOptions<AppDbContext>));

            if (dbContextDescriptor != null)
                services.Remove(dbContextDescriptor);

            var sp = services.BuildServiceProvider();
            var config = sp.GetRequiredService<IConfiguration>();

            var cs = config.GetConnectionString("DefaultConnection") 
                     ?? "Host=localhost;Port=5433;Database=ecommerce_test;Username=ecommerce_test_user;Password=test_password";

            services.AddDbContext<AppDbContext>(opt =>
                opt.UseNpgsql(cs));

            // Replace JWT Authentication with TestAuthHandler
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = TestAuthHandler.AuthenticationScheme;
                options.DefaultChallengeScheme = TestAuthHandler.AuthenticationScheme;
            })
            .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
                TestAuthHandler.AuthenticationScheme, options => { });
        });
    }
}

// helper for seeding test data
public static class TestDataSeeder
{
    // Ensures user exists in db
    public static async Task<User> EnsureUserAsync(AppDbContext db, int userId, string role = "Customer")
    {
        var user = await db.Users.FindAsync(userId);
        if (user != null) return user;

        // Ensure role exists
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

    // Ensures category exists
    public static async Task<Category> EnsureCategoryAsync(AppDbContext db, int categoryId = 1, string name = "Test Category")
    {
        var category = await db.Categories.FindAsync(categoryId);
        if (category != null) return category;

        category = new Category
        {
            Id = categoryId,
            Name = name,
            Description = "Category for integration tests",
            IsActive = true
        };
        
        db.Categories.Add(category);
        await db.SaveChangesAsync();
        
        return category;
    }

    // Ensures product with stock exists
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
}
