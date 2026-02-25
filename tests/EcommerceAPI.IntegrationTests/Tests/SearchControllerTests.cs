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
        var unique = $"es-search-{Guid.NewGuid():N}"[..20];
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

            seededProduct.Name = $"Search Test {unique}";
            seededProduct.SKU = unique.ToUpperInvariant();
            seededProduct.IsActive = true;
            await db.SaveChangesAsync();

            await searchIndexService.IndexProductAsync(seededProduct.Id);
        }

        var searchClient = _factory.CreateClient();
        var searchTerm = Uri.EscapeDataString(unique);

        var searchResponse = await searchClient.GetAsync($"/api/v1/search/products?q={searchTerm}&page=1&pageSize=10");

        searchResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var searchResult = await searchResponse.Content.ReadFromJsonAsync<ApiResult<PaginatedResponse<ProductDto>>>();
        searchResult.Should().NotBeNull();
        searchResult!.Success.Should().BeTrue();
        searchResult.Data.Items.Should().Contain(p => p.SKU == unique.ToUpperInvariant());
    }
}
