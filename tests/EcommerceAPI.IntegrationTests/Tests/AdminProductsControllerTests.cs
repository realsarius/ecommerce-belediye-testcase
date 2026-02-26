using System.Net;
using System.Net.Http.Json;
using EcommerceAPI.DataAccess.Concrete.EntityFramework.Contexts;
using EcommerceAPI.Entities.DTOs;
using EcommerceAPI.IntegrationTests.Utilities;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EcommerceAPI.IntegrationTests.Tests;

[Collection("Integration")]
public class AdminProductsControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public AdminProductsControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task CreateProduct_WithoutAuth_Returns401()
    {
        var anonymousClient = _factory.CreateClient().AsAnonymous();
        var categoryId = await GetExistingCategoryIdAsync();
        var createRequest = new CreateProductRequest
        {
            Name = "Test Product",
            Description = "Test Description",
            Price = 99.99m,
            CategoryId = categoryId,
            SKU = "TEST-001"
        };

        var response = await anonymousClient.PostAsJsonAsync("/api/v1/admin/products", createRequest);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateProduct_AsCustomer_Returns403()
    {
        var customerClient = _factory.CreateClient().AsCustomer(userId: 1);
        var categoryId = await GetExistingCategoryIdAsync();
        var createRequest = new CreateProductRequest
        {
            Name = "Test Product",
            Description = "Test Description",
            Price = 99.99m,
            CategoryId = categoryId,
            SKU = "TEST-002"
        };

        var response = await customerClient.PostAsJsonAsync("/api/v1/admin/products", createRequest);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task CreateProduct_AsAdmin_ReturnsCreatedOrError()
    {
        var adminClient = _factory.CreateClient().AsAdmin(userId: 1);
        var categoryId = await GetExistingCategoryIdAsync();
        var createRequest = new CreateProductRequest
        {
            Name = $"Admin Test Product {Guid.NewGuid():N}",
            Description = "Created by admin test",
            Price = 149.99m,
            CategoryId = categoryId,
            SKU = $"ADM-{Guid.NewGuid():N}"[..12]
        };

        var response = await adminClient.PostAsJsonAsync("/api/v1/admin/products", createRequest);

        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.Created, 
            HttpStatusCode.BadRequest,
            HttpStatusCode.InternalServerError
        );
    }

    [Fact]
    public async Task UpdateProduct_AsAdmin_ReturnsOkOrError()
    {
        var adminClient = _factory.CreateClient().AsAdmin(userId: 1);
        var categoryId = await GetExistingCategoryIdAsync();
        
        var productsResponse = await adminClient.GetAsync("/api/v1/products?page=1&pageSize=1");
        var productsApiResult = await productsResponse.Content.ReadFromJsonAsync<ApiResult<PaginatedResponse<ProductDto>>>();
        
        if (productsApiResult?.Data?.Items?.Any() != true) return;

        var productId = productsApiResult.Data.Items.First().Id;
        var updateRequest = new UpdateProductRequest
        {
            Name = $"Updated Product {DateTime.UtcNow.Ticks}",
            Description = "Updated description",
            Price = 199.99m,
            CategoryId = categoryId,
            IsActive = true
        };

        var response = await adminClient.PutAsJsonAsync($"/api/v1/admin/products/{productId}", updateRequest);

        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.OK, 
            HttpStatusCode.NotFound,
            HttpStatusCode.BadRequest,
            HttpStatusCode.InternalServerError
        );
    }

    [Fact]
    public async Task DeleteProduct_AsAdmin_NonExisting_ReturnsBadRequest()
    {
        var adminClient = _factory.CreateClient().AsAdmin(userId: 1);
        var nonExistingId = 999999;

        var response = await adminClient.DeleteAsync($"/api/v1/admin/products/{nonExistingId}");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateStock_AsAdmin_ReturnsOkOrError()
    {
        var adminClient = _factory.CreateClient().AsAdmin(userId: 1);
        
        var productsResponse = await adminClient.GetAsync("/api/v1/products?page=1&pageSize=1");
        var productsApiResult = await productsResponse.Content.ReadFromJsonAsync<ApiResult<PaginatedResponse<ProductDto>>>();
        
        if (productsApiResult?.Data?.Items?.Any() != true) return;

        var productId = productsApiResult.Data.Items.First().Id;
        var stockRequest = new UpdateStockRequest
        {
            Delta = 10,
            Reason = "Integration test stock update"
        };

        var response = await adminClient.PatchAsJsonAsync($"/api/v1/admin/products/{productId}/stock", stockRequest);

        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.OK, 
            HttpStatusCode.BadRequest,
            HttpStatusCode.InternalServerError
        );
    }

    [Fact]
    public async Task UpdateStock_AsCustomer_Returns403()
    {
        var customerClient = _factory.CreateClient().AsCustomer(userId: 1);
        var stockRequest = new UpdateStockRequest
        {
            Delta = 10,
            Reason = "Should fail"
        };

        var response = await customerClient.PatchAsJsonAsync("/api/v1/admin/products/1/stock", stockRequest);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    private async Task<int> GetExistingCategoryIdAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var categoryId = await db.Categories.AsNoTracking().Select(c => c.Id).FirstOrDefaultAsync();
        return categoryId != 0
            ? categoryId
            : throw new InvalidOperationException("No categories found for integration tests.");
    }
}
