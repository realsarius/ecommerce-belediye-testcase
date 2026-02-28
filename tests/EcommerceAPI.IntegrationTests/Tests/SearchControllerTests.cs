using System.Net;
using System.Net.Http.Json;
using EcommerceAPI.Business.Abstract;
using EcommerceAPI.DataAccess.Concrete.EntityFramework.Contexts;
using EcommerceAPI.Entities.DTOs;
using EcommerceAPI.IntegrationTests.Utilities;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace EcommerceAPI.IntegrationTests.Tests;

[Collection("Integration")]
public class SearchControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public SearchControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task SearchProducts_DefaultPagination_ReturnsOk()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/v1/search/products?page=1&pageSize=5");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var apiResult = await response.Content.ReadFromJsonAsync<ApiResult<PaginatedResponse<ProductDto>>>();
        apiResult.Should().NotBeNull();
        apiResult!.Success.Should().BeTrue();
        apiResult.Data.Should().NotBeNull();
        apiResult.Data.Page.Should().Be(1);
        apiResult.Data.PageSize.Should().Be(5);
    }

    [Fact]
    public async Task SearchProducts_WithQuery_ReturnsMatchingProduct()
    {
        var unique = $"q{Guid.NewGuid():N}"[..12].ToLowerInvariant();
        var productId = Random.Shared.Next(1_000_000, 1_500_000);
        var categoryId = Random.Shared.Next(500_000, 900_000);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var searchIndexService = scope.ServiceProvider.GetRequiredService<IProductSearchIndexService>();

            await TestDataSeeder.EnsureCategoryAsync(db, categoryId, $"Search Category {unique}");
            var seededProduct = await TestDataSeeder.EnsureProductWithStockAsync(
                db,
                productId: productId,
                categoryId: categoryId,
                stockQuantity: 13);

            seededProduct.Name = $"SearchTest {unique}";
            seededProduct.SKU = unique.ToUpperInvariant();
            seededProduct.IsActive = true;
            await db.SaveChangesAsync();

            await searchIndexService.IndexProductAsync(seededProduct.Id);
        }

        var searchResult = await WaitForSearchAsync(
            unique,
            result => result.Items.Any(p => p.SKU == unique.ToUpperInvariant()));

        searchResult.Items.Should().Contain(p => p.SKU == unique.ToUpperInvariant());
    }

    [Fact]
    public async Task SearchProducts_ExactNameMatch_ShouldRankHigherThanDescriptionOnlyMatch()
    {
        var unique = $"r{Guid.NewGuid():N}"[..12].ToLowerInvariant();
        var exactProductId = Random.Shared.Next(1_500_001, 1_600_000);
        var looseProductId = Random.Shared.Next(1_600_001, 1_700_000);
        var categoryId = Random.Shared.Next(900_001, 950_000);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var searchIndexService = scope.ServiceProvider.GetRequiredService<IProductSearchIndexService>();

            await TestDataSeeder.EnsureCategoryAsync(db, categoryId, $"Rank Category {unique}");

            var exactProduct = await TestDataSeeder.EnsureProductWithStockAsync(
                db,
                productId: exactProductId,
                categoryId: categoryId,
                stockQuantity: 10);

            exactProduct.Name = unique;
            exactProduct.Description = "Exact match product";
            exactProduct.SKU = $"EXACT-{unique}".ToUpperInvariant();
            exactProduct.IsActive = true;

            var looseProduct = await TestDataSeeder.EnsureProductWithStockAsync(
                db,
                productId: looseProductId,
                categoryId: categoryId,
                stockQuantity: 10);

            looseProduct.Name = "Generic Product";
            looseProduct.Description = $"Description contains {unique}";
            looseProduct.SKU = $"LOOSE-{unique}".ToUpperInvariant();
            looseProduct.IsActive = true;

            await db.SaveChangesAsync();

            await searchIndexService.IndexProductAsync(exactProduct.Id);
            await searchIndexService.IndexProductAsync(looseProduct.Id);
        }

        var result = await WaitForSearchAsync(
            unique,
            data => data.Items.Any() && data.Items.First().Id == exactProductId);

        result.Items.Should().NotBeEmpty();
        result.Items.First().Id.Should().Be(exactProductId);
        result.Items.Should().Contain(x => x.Id == looseProductId);
    }

    [Fact]
    public async Task SearchProducts_WithMinorTypo_ShouldStillReturnMatchingProduct()
    {
        var unique = $"t{Guid.NewGuid():N}"[..12].ToLowerInvariant();
        var productId = Random.Shared.Next(1_700_001, 1_800_000);
        var categoryId = Random.Shared.Next(950_001, 990_000);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var searchIndexService = scope.ServiceProvider.GetRequiredService<IProductSearchIndexService>();

            await TestDataSeeder.EnsureCategoryAsync(db, categoryId, $"Typo Category {unique}");

            var product = await TestDataSeeder.EnsureProductWithStockAsync(
                db,
                productId: productId,
                categoryId: categoryId,
                stockQuantity: 8);

            product.Name = unique;
            product.Description = "Typo tolerance test";
            product.SKU = $"TYPO-{unique}".ToUpperInvariant();
            product.IsActive = true;

            await db.SaveChangesAsync();

            await searchIndexService.IndexProductAsync(product.Id);
        }

        var typoQuery = unique.Remove(4, 1);
        var result = await WaitForSearchAsync(
            typoQuery,
            data => data.Items.Any(x => x.Id == productId));

        result.Items.Should().Contain(x => x.Id == productId);
    }

    [Fact]
    public async Task SearchProducts_ShouldReturnWishlistCountFromIndex()
    {
        var unique = $"w{Guid.NewGuid():N}"[..12].ToLowerInvariant();
        var productId = Random.Shared.Next(1_800_001, 1_900_000);
        var categoryId = Random.Shared.Next(990_001, 1_050_000);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var searchIndexService = scope.ServiceProvider.GetRequiredService<IProductSearchIndexService>();

            await TestDataSeeder.EnsureCategoryAsync(db, categoryId, $"Wishlist Category {unique}");

            var product = await TestDataSeeder.EnsureProductWithStockAsync(
                db,
                productId: productId,
                categoryId: categoryId,
                stockQuantity: 6);

            product.Name = $"WishlistCount {unique}";
            product.Description = "Wishlist count sync test";
            product.SKU = $"WISH-{unique}".ToUpperInvariant();
            product.IsActive = true;
            product.WishlistCount = 9;

            await db.SaveChangesAsync();
            await searchIndexService.IndexProductAsync(product.Id);
        }

        var result = await WaitForSearchAsync(
            unique,
            data => data.Items.Any(x => x.Id == productId && x.WishlistCount == 9));

        result.Items.Should().Contain(x => x.Id == productId && x.WishlistCount == 9);
    }

    [Fact]
    public async Task SearchProducts_ShouldSortByWishlistCountDescending()
    {
        var unique = $"s{Guid.NewGuid():N}"[..12].ToLowerInvariant();
        var firstProductId = Random.Shared.Next(1_900_001, 2_000_000);
        var secondProductId = Random.Shared.Next(2_000_001, 2_100_000);
        var categoryId = Random.Shared.Next(1_050_001, 1_150_000);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var searchIndexService = scope.ServiceProvider.GetRequiredService<IProductSearchIndexService>();

            await TestDataSeeder.EnsureCategoryAsync(db, categoryId, $"Sort Category {unique}");

            var higherWishlisted = await TestDataSeeder.EnsureProductWithStockAsync(db, firstProductId, categoryId, 7);
            higherWishlisted.Name = $"Wish Sort A {unique}";
            higherWishlisted.SKU = $"SORT-A-{unique}".ToUpperInvariant();
            higherWishlisted.IsActive = true;
            higherWishlisted.WishlistCount = 20;

            var lowerWishlisted = await TestDataSeeder.EnsureProductWithStockAsync(db, secondProductId, categoryId, 7);
            lowerWishlisted.Name = $"Wish Sort B {unique}";
            lowerWishlisted.SKU = $"SORT-B-{unique}".ToUpperInvariant();
            lowerWishlisted.IsActive = true;
            lowerWishlisted.WishlistCount = 3;

            await db.SaveChangesAsync();
            await searchIndexService.IndexProductAsync(higherWishlisted.Id);
            await searchIndexService.IndexProductAsync(lowerWishlisted.Id);
        }

        var result = await WaitForSearchAsync(
            unique,
            data => data.Items.Count >= 2 &&
                    data.Items.Any(x => x.Id == firstProductId && x.WishlistCount == 20) &&
                    data.Items.Any(x => x.Id == secondProductId && x.WishlistCount == 3),
            requestUri: $"/api/v1/search/products?q={Uri.EscapeDataString(unique)}&page=1&pageSize=10&sortBy=wishlistCount&sortDescending=true");

        result.Items.Select(item => item.Id).Should().ContainInOrder(firstProductId, secondProductId);
    }

    private async Task<PaginatedResponse<ProductDto>> WaitForSearchAsync(
        string query,
        Func<PaginatedResponse<ProductDto>, bool> predicate,
        string? requestUri = null,
        int retries = 8,
        int delayMs = 250)
    {
        var client = _factory.CreateClient();
        var uri = requestUri ?? $"/api/v1/search/products?q={Uri.EscapeDataString(query)}&page=1&pageSize=20";

        for (var attempt = 0; attempt < retries; attempt++)
        {
            var response = await client.GetAsync(uri);
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var result = await response.Content.ReadFromJsonAsync<ApiResult<PaginatedResponse<ProductDto>>>();
            result.Should().NotBeNull();
            result!.Success.Should().BeTrue();

            if (predicate(result.Data))
            {
                return result.Data;
            }

            await Task.Delay(delayMs);
        }

        var finalResponse = await client.GetAsync(uri);
        finalResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var finalResult = await finalResponse.Content.ReadFromJsonAsync<ApiResult<PaginatedResponse<ProductDto>>>();
        finalResult.Should().NotBeNull();
        finalResult!.Success.Should().BeTrue();
        return finalResult.Data;
    }
}
