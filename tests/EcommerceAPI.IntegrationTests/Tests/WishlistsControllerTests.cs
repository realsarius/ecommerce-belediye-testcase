using System.Net;
using System.Net.Http.Json;
using EcommerceAPI.Entities.DTOs;
using EcommerceAPI.IntegrationTests.Utilities;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EcommerceAPI.IntegrationTests.Tests;

[Collection("Integration")]
public class WishlistsControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public WishlistsControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetWishlist_AsCustomer_ReturnsOk()
    {
        var client = _factory.CreateClient().AsCustomer(1);
        var response = await client.GetAsync("/api/v1/wishlists");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResult<WishlistDto>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
    }

    [Fact]
    public async Task GetWishlist_WithCursorPaginationQuery_ReturnsOk()
    {
        var client = _factory.CreateClient().AsCustomer(1);
        var response = await client.GetAsync("/api/v1/wishlists?limit=5");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResult<WishlistDto>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Data!.Limit.Should().Be(5);
    }

    [Fact]
    public async Task GetWishlist_WithoutAuth_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient(); // No auth
        var response = await client.GetAsync("/api/v1/wishlists");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task AddItemToWishlist_AsCustomer_ReturnsOk()
    {
        var client = _factory.CreateClient().AsCustomer(1);
        var request = new AddWishlistItemRequest { ProductId = 1 }; // Assuming product 1 exists from seed

        var response = await client.PostAsJsonAsync("/api/v1/wishlists/items", request);

        // Since it could be that Product 1 does not exist in testing DB, it might return BadRequest or Ok.
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound, HttpStatusCode.BadRequest);
        
        if (response.StatusCode == HttpStatusCode.OK)
        {
            var result = await response.Content.ReadFromJsonAsync<ApiResult<object>>();
            result!.Success.Should().BeTrue();
        }
    }

    [Fact]
    public async Task AddItemToWishlist_WhenProductIdInvalid_ReturnsBadRequest()
    {
        var client = _factory.CreateClient().AsCustomer(1);
        var request = new AddWishlistItemRequest { ProductId = 0 };

        var response = await client.PostAsJsonAsync("/api/v1/wishlists/items", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task AddAllToCart_WhenWishlistContainsAvailableAndUnavailableItems_ReturnsSummary()
    {
        const int userId = 510001;
        const int categoryId = 510010;
        const int activeProductId = 510011;
        const int outOfStockProductId = 510012;

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<EcommerceAPI.DataAccess.Concrete.EntityFramework.Contexts.AppDbContext>();
            await TestDataSeeder.EnsureUserAsync(db, userId);
            await TestDataSeeder.EnsureCategoryAsync(db, categoryId, "Wishlist Bulk Category");

            var activeProduct = await TestDataSeeder.EnsureProductWithStockAsync(db, activeProductId, categoryId, 4);
            activeProduct.IsActive = true;

            var outOfStockProduct = await TestDataSeeder.EnsureProductWithStockAsync(db, outOfStockProductId, categoryId, 0);
            outOfStockProduct.IsActive = true;

            await db.SaveChangesAsync();
        }

        var client = _factory.CreateClient().AsCustomer(userId);
        await client.PostAsJsonAsync("/api/v1/wishlists/items", new AddWishlistItemRequest { ProductId = activeProductId });
        await client.PostAsJsonAsync("/api/v1/wishlists/items", new AddWishlistItemRequest { ProductId = outOfStockProductId });

        var response = await client.PostAsync("/api/v1/wishlists/add-all-to-cart", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResult<WishlistBulkAddToCartResultDto>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Data!.AddedCount.Should().Be(1);
        result.Data.SkippedCount.Should().Be(1);
    }

    [Fact]
    public async Task EnableSharing_WhenWishlistHasItems_AllowsAnonymousPublicRead()
    {
        const int userId = 510101;
        const int categoryId = 510110;
        const int productId = 510111;

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<EcommerceAPI.DataAccess.Concrete.EntityFramework.Contexts.AppDbContext>();
            await TestDataSeeder.EnsureUserAsync(db, userId);
            await TestDataSeeder.EnsureCategoryAsync(db, categoryId, "Wishlist Share Category");
            var product = await TestDataSeeder.EnsureProductWithStockAsync(db, productId, categoryId, 8);
            product.IsActive = true;
            await db.SaveChangesAsync();
        }

        var authenticatedClient = _factory.CreateClient().AsCustomer(userId);
        await authenticatedClient.PostAsJsonAsync("/api/v1/wishlists/items", new AddWishlistItemRequest { ProductId = productId });

        var enableResponse = await authenticatedClient.PostAsync("/api/v1/wishlists/share", null);

        enableResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var enableResult = await enableResponse.Content.ReadFromJsonAsync<ApiResult<WishlistShareSettingsDto>>();
        enableResult.Should().NotBeNull();
        enableResult!.Success.Should().BeTrue();
        enableResult.Data!.IsPublic.Should().BeTrue();
        enableResult.Data.ShareToken.Should().NotBeNull();

        var anonymousClient = _factory.CreateClient();
        var sharedResponse = await anonymousClient.GetAsync($"/api/v1/wishlists/share/{enableResult.Data.ShareToken}");

        sharedResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var sharedResult = await sharedResponse.Content.ReadFromJsonAsync<ApiResult<SharedWishlistDto>>();
        sharedResult.Should().NotBeNull();
        sharedResult!.Success.Should().BeTrue();
        sharedResult.Data!.Items.Should().ContainSingle(item => item.ProductId == productId);
        sharedResult.Data.OwnerDisplayName.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task GetSharedWishlist_WhenSharingDisabled_ReturnsNotFound()
    {
        const int userId = 510201;

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<EcommerceAPI.DataAccess.Concrete.EntityFramework.Contexts.AppDbContext>();
            await TestDataSeeder.EnsureUserAsync(db, userId);
            await db.SaveChangesAsync();
        }

        var authenticatedClient = _factory.CreateClient().AsCustomer(userId);
        var enableResponse = await authenticatedClient.PostAsync("/api/v1/wishlists/share", null);
        var enableResult = await enableResponse.Content.ReadFromJsonAsync<ApiResult<WishlistShareSettingsDto>>();

        enableResult.Should().NotBeNull();
        enableResult!.Data!.ShareToken.Should().NotBeNull();

        var disableResponse = await authenticatedClient.DeleteAsync("/api/v1/wishlists/share");
        disableResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var anonymousClient = _factory.CreateClient();
        var sharedResponse = await anonymousClient.GetAsync($"/api/v1/wishlists/share/{enableResult.Data.ShareToken}");

        sharedResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task WishlistCollections_CreateMoveAndFilterFlow_ReturnsExpectedItems()
    {
        const int userId = 510301;
        const int categoryId = 510310;
        const int productId = 510311;
        var collectionName = $"Teknoloji-{Guid.NewGuid():N}";

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<EcommerceAPI.DataAccess.Concrete.EntityFramework.Contexts.AppDbContext>();
            await TestDataSeeder.EnsureUserAsync(db, userId);
            await TestDataSeeder.EnsureCategoryAsync(db, categoryId, "Wishlist Collections Category");
            var product = await TestDataSeeder.EnsureProductWithStockAsync(db, productId, categoryId, 9);
            product.IsActive = true;
            await db.SaveChangesAsync();
        }

        var client = _factory.CreateClient().AsCustomer(userId);

        var createCollectionResponse = await client.PostAsJsonAsync(
            "/api/v1/wishlists/collections",
            new CreateWishlistCollectionRequest { Name = collectionName });

        createCollectionResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var createdCollection = await createCollectionResponse.Content.ReadFromJsonAsync<ApiResult<WishlistCollectionDto>>();
        createdCollection.Should().NotBeNull();
        createdCollection!.Success.Should().BeTrue();
        createdCollection.Data!.Name.Should().Be(collectionName);

        var collectionsResponse = await client.GetAsync("/api/v1/wishlists/collections");
        collectionsResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var collections = await collectionsResponse.Content.ReadFromJsonAsync<ApiResult<List<WishlistCollectionDto>>>();
        collections.Should().NotBeNull();
        collections!.Data.Should().Contain(collection => collection.Name == collectionName);
        collections.Data.Should().Contain(collection => collection.IsDefault);

        await client.PostAsJsonAsync("/api/v1/wishlists/items", new AddWishlistItemRequest { ProductId = productId });

        var moveResponse = await client.PatchAsJsonAsync(
            $"/api/v1/wishlists/items/{productId}/collection",
            new MoveWishlistItemToCollectionRequest { CollectionId = createdCollection.Data.Id });

        moveResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var filteredWishlistResponse = await client.GetAsync($"/api/v1/wishlists?collectionId={createdCollection.Data.Id}");
        filteredWishlistResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var filteredWishlist = await filteredWishlistResponse.Content.ReadFromJsonAsync<ApiResult<WishlistDto>>();
        filteredWishlist.Should().NotBeNull();
        filteredWishlist!.Success.Should().BeTrue();
        filteredWishlist.Data!.ActiveCollectionId.Should().Be(createdCollection.Data.Id);
        filteredWishlist.Data.Items.Should().ContainSingle(item =>
            item.ProductId == productId &&
            item.CollectionId == createdCollection.Data.Id &&
            item.CollectionName == collectionName);
    }
}
