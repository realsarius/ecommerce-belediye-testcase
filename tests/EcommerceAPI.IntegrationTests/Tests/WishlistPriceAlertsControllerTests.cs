using System.Net;
using System.Net.Http.Json;
using EcommerceAPI.DataAccess.Concrete.EntityFramework.Contexts;
using EcommerceAPI.Entities.DTOs;
using EcommerceAPI.IntegrationTests.Utilities;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace EcommerceAPI.IntegrationTests.Tests;

[Collection("Integration")]
public class WishlistPriceAlertsControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public WishlistPriceAlertsControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task PriceAlertCrudFlow_WhenProductInWishlist_ReturnsOk()
    {
        const int userId = 501001;
        const int productId = 501002;
        const int categoryId = 501003;

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await TestDataSeeder.EnsureUserAsync(db, userId);
            await TestDataSeeder.EnsureCategoryAsync(db, categoryId, "Wishlist Alert Category");
            await TestDataSeeder.EnsureProductWithStockAsync(db, productId, categoryId, 10);
        }

        var client = _factory.CreateClient().AsCustomer(userId);
        var addWishlistResponse = await client.PostAsJsonAsync("/api/v1/wishlists/items", new AddWishlistItemRequest
        {
            ProductId = productId
        });

        addWishlistResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var upsertResponse = await client.PutAsJsonAsync("/api/v1/wishlists/price-alerts", new UpsertWishlistPriceAlertRequest
        {
            ProductId = productId,
            TargetPrice = 49.99m
        });

        upsertResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var upsertResult = await upsertResponse.Content.ReadFromJsonAsync<ApiResult<WishlistPriceAlertDto>>();
        upsertResult.Should().NotBeNull();
        upsertResult!.Success.Should().BeTrue();
        upsertResult.Data!.ProductId.Should().Be(productId);
        upsertResult.Data.TargetPrice.Should().Be(49.99m);

        var listResponse = await client.GetAsync("/api/v1/wishlists/price-alerts");
        listResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var listResult = await listResponse.Content.ReadFromJsonAsync<ApiResult<List<WishlistPriceAlertDto>>>();
        listResult.Should().NotBeNull();
        listResult!.Success.Should().BeTrue();
        listResult.Data.Should().Contain(alert => alert.ProductId == productId && alert.TargetPrice == 49.99m);

        var deleteResponse = await client.DeleteAsync($"/api/v1/wishlists/price-alerts/{productId}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
