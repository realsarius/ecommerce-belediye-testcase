using System.Net;
using System.Net.Http.Json;
using EcommerceAPI.DataAccess.Concrete.EntityFramework.Contexts;
using EcommerceAPI.Entities.Concrete;
using EcommerceAPI.Entities.DTOs;
using EcommerceAPI.Entities.Enums;
using EcommerceAPI.IntegrationTests.Utilities;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EcommerceAPI.IntegrationTests.Tests;

[Collection("Integration")]
public class ProductsControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public ProductsControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetProduct_WhenProductDoesNotExist_ReturnsBadRequest()
    {
        var nonExistingId = Random.Shared.Next(950_001, 951_000);
        var response = await _client.GetAsync($"/api/v1/products/{nonExistingId}");
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetProduct_WhenProductExists_ReturnsOk()
    {
        var categoryId = Random.Shared.Next(951_001, 952_000);
        var productId = Random.Shared.Next(952_001, 953_000);

        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await TestDataSeeder.EnsureProductWithStockAsync(db, productId, categoryId, 25);
        }

        var response = await _client.GetAsync($"/api/v1/products/{productId}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetFrequentlyBoughtRecommendations_WhenOrderHistoryExists_ReturnsRelatedProducts()
    {
        var userId = Random.Shared.Next(953_001, 954_000);
        var categoryId = Random.Shared.Next(954_001, 955_000);
        var sourceProductId = Random.Shared.Next(955_001, 956_000);
        var relatedProductId = Random.Shared.Next(956_001, 957_000);
        var orderId = Random.Shared.Next(957_001, 958_000);

        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await TestDataSeeder.EnsureUserAsync(db, userId);
            await TestDataSeeder.EnsureCategoryAsync(db, categoryId);
            await TestDataSeeder.EnsureProductWithStockAsync(db, sourceProductId, categoryId, 15);
            await TestDataSeeder.EnsureProductWithStockAsync(db, relatedProductId, categoryId, 15);

            if (!await db.Orders.AnyAsync(o => o.Id == orderId))
            {
                var order = new Order
                {
                    Id = orderId,
                    UserId = userId,
                    OrderNumber = $"REC-{orderId}",
                    Status = OrderStatus.Delivered,
                    TotalAmount = 199.98m,
                    ShippingAddress = "Test Address"
                };

                order.OrderItems.Add(new OrderItem { ProductId = sourceProductId, Quantity = 1, PriceSnapshot = 99.99m });
                order.OrderItems.Add(new OrderItem { ProductId = relatedProductId, Quantity = 1, PriceSnapshot = 99.99m });
                order.Payment = new Payment
                {
                    Amount = 199.98m,
                    Currency = "TRY",
                    Status = PaymentStatus.Success,
                    PaymentMethod = "CreditCard",
                    IdempotencyKey = $"recommendation-payment-{orderId}"
                };

                db.Orders.Add(order);
                await db.SaveChangesAsync();
            }
        }

        var response = await _client.GetAsync($"/api/v1/products/{sourceProductId}/recommendations/frequently-bought?take=4");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ApiResult<List<ProductDto>>>();
        result.Should().NotBeNull();
        result!.Data.Should().ContainSingle(item => item.Id == relatedProductId);
    }

    [Fact]
    public async Task GetAlsoViewedRecommendations_WhenRedisEmpty_ReturnsCategoryFallback()
    {
        var categoryId = Random.Shared.Next(958_001, 959_000);
        var sourceProductId = Random.Shared.Next(959_001, 960_000);
        var relatedProductId = Random.Shared.Next(960_001, 961_000);

        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await TestDataSeeder.EnsureCategoryAsync(db, categoryId);
            var sourceProduct = await TestDataSeeder.EnsureProductWithStockAsync(db, sourceProductId, categoryId, 10);
            var relatedProduct = await TestDataSeeder.EnsureProductWithStockAsync(db, relatedProductId, categoryId, 10);

            sourceProduct.WishlistCount = 1;
            relatedProduct.WishlistCount = 20;
            db.Products.Update(sourceProduct);
            db.Products.Update(relatedProduct);
            await db.SaveChangesAsync();
        }

        var response = await _client.GetAsync($"/api/v1/products/{sourceProductId}/recommendations/also-viewed?take=4");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ApiResult<List<ProductDto>>>();
        result.Should().NotBeNull();
        result!.Data.Should().Contain(item => item.Id == relatedProductId);
    }
}
