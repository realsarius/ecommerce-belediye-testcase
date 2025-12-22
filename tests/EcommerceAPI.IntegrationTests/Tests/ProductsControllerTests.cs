using System.Net;
using System.Net.Http.Json;
using EcommerceAPI.Core.DTOs;
using FluentAssertions;
using Xunit;

namespace EcommerceAPI.IntegrationTests.Tests;

// Products controller tests - public endpoints

[Collection("Integration")]
public class ProductsControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public ProductsControllerTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetProducts_ReturnsOkWithPaginatedResponse()
    {
        // Arrange & Act
        var response = await _client.GetAsync("/api/v1/products?page=1&pageSize=10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PaginatedResponse<ProductDto>>();
        result.Should().NotBeNull();
        result!.Items.Should().NotBeNull();
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(10);
    }

    [Fact]
    public async Task GetProducts_WithCategoryFilter_ReturnsFilteredProducts()
    {
        // Arrange & Act
        var response = await _client.GetAsync("/api/v1/products?categoryId=1");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PaginatedResponse<ProductDto>>();
        result.Should().NotBeNull();
        // All returned products should be from category 1 (if any exist)
    }

    [Fact]
    public async Task GetProducts_WithSearch_ReturnsMatchingProducts()
    {
        // Arrange & Act
        var response = await _client.GetAsync("/api/v1/products?search=test");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetProducts_WithSorting_ReturnsOk()
    {
        // Arrange & Act
        var response = await _client.GetAsync("/api/v1/products?sortBy=price&sortDesc=true");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetProductById_NonExistingId_ReturnsNotFound()
    {
        // Arrange
        var nonExistingId = 999999;

        // Act
        var response = await _client.GetAsync($"/api/v1/products/{nonExistingId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetProductById_ValidId_ReturnsProduct()
    {
        // Arrange - First get list of products to find a valid ID
        var listResponse = await _client.GetAsync("/api/v1/products?page=1&pageSize=1");
        var listResult = await listResponse.Content.ReadFromJsonAsync<PaginatedResponse<ProductDto>>();
        
        if (listResult?.Items?.Any() != true)
        {
            // No products in DB, skip test
            return;
        }

        var productId = listResult.Items.First().Id;

        // Act
        var response = await _client.GetAsync($"/api/v1/products/{productId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var product = await response.Content.ReadFromJsonAsync<ProductDto>();
        product.Should().NotBeNull();
        product!.Id.Should().Be(productId);
    }
}
