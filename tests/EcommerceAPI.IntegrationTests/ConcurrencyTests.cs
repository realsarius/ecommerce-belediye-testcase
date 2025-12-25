using System.Net.Http.Json;
using Xunit;
using FluentAssertions;
using EcommerceAPI.IntegrationTests.Utilities;
using Microsoft.Extensions.DependencyInjection;
using EcommerceAPI.DataAccess.Concrete.EntityFramework.Contexts;
using EcommerceAPI.Entities.Concrete;
using Xunit.Abstractions;

namespace EcommerceAPI.IntegrationTests;

public class ConcurrencyTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;
    private readonly ITestOutputHelper _output;

    public ConcurrencyTests(CustomWebApplicationFactory factory, ITestOutputHelper output)
    {
        _factory = factory;
        _client = factory.CreateClient();
        _output = output;
    }

    [Fact]
    public async Task DecreaseStock_ConcurrentRequests_ShouldPreventOverselling()
    {
        // 1. Setup Data
        var random = new Random();
        int productId = random.Next(100000, 999999);
        int categoryId = random.Next(100000, 999999);
        int initialStock = 1;

        var userIds = Enumerable.Range(0, 5).Select(_ => random.Next(100000, 999999)).ToList();

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            
            // Seed Category & Product
            await TestDataSeeder.EnsureCategoryAsync(db, categoryId, $"Cat-{categoryId}");
            await TestDataSeeder.EnsureProductWithStockAsync(db, productId, categoryId, initialStock);

            // Seed Users
            foreach (var uid in userIds)
            {
                await TestDataSeeder.EnsureUserAsync(db, uid);
            }
        }

        // 2. Action: 5 concurrent threads (different users)
        var tasks = new List<Task<HttpResponseMessage>>();
        
        foreach (var userId in userIds)
        {
            tasks.Add(Task.Run(async () =>
            {
                // Create unique client for this user/thread
                var client = _factory.CreateClient();
                client.DefaultRequestHeaders.Add(TestAuthHandler.UserIdHeader, userId.ToString());
                client.DefaultRequestHeaders.Add(TestAuthHandler.RoleHeader, "Customer");

                // Add to Cart
                var addRes = await client.PostAsJsonAsync("/api/v1/Cart/items", new { productId = productId, quantity = 1 });
                if (!addRes.IsSuccessStatusCode) return addRes;

                // Checkout
                return await client.PostAsJsonAsync("/api/v1/Orders", new 
                { 
                    shippingAddress = "Test Address",
                    paymentMethod = "CreditCard",
                    notes = $"Concurrency Test {Guid.NewGuid()}"
                });
            }));
        }

        var responses = await Task.WhenAll(tasks);
        
        // 4. Assertions
        var successCount = responses.Count(r => r.IsSuccessStatusCode);
        
        if (successCount != 1) 
        {
            _output.WriteLine($"Assertion Failed. Success Count: {successCount}");
            foreach(var r in responses)
            {
                var content = await r.Content.ReadAsStringAsync();
                _output.WriteLine($"Status: {r.StatusCode}, Content: {content}");
            }
        }

        successCount.Should().Be(1);
    }
}
