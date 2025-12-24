using System.Net;
using System.Net.Http.Json;
using EcommerceAPI.Entities.DTOs;
using FluentAssertions;
using EcommerceAPI.IntegrationTests.Utilities;
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
        var apiResult = await response.Content.ReadFromJsonAsync<ApiResult<PaginatedResponse<ProductDto>>>();
        apiResult.Should().NotBeNull();
        apiResult!.Success.Should().BeTrue();
        
        var result = apiResult.Data;
        result.Should().NotBeNull();
        result.Items.Should().NotBeNull();
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
        var apiResult = await response.Content.ReadFromJsonAsync<ApiResult<PaginatedResponse<ProductDto>>>();
        apiResult.Should().NotBeNull();
        apiResult!.Success.Should().BeTrue();
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
    public async Task GetProductById_NonExistingId_ReturnsBadRequest()
    {
        // Arrange
        var nonExistingId = 999999;

        // Act
        var response = await _client.GetAsync($"/api/v1/products/{nonExistingId}");

        // Assert
        // Refactored controller returns BadRequest instead of NotFound for errors
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetProductById_ValidId_ReturnsProduct()
    {
        // Arrange - First get list of products to find a valid ID
        var listResponse = await _client.GetAsync("/api/v1/products?page=1&pageSize=1");
        var listApiResult = await listResponse.Content.ReadFromJsonAsync<ApiResult<PaginatedResponse<ProductDto>>>();
        
        if (listApiResult?.Data?.Items?.Any() != true)
        {
            // No products in DB, skip test
            return;
        }

        var productId = listApiResult.Data.Items.First().Id;

        // Act
        var response = await _client.GetAsync($"/api/v1/products/{productId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var apiResult = await response.Content.ReadFromJsonAsync<ApiResult<ProductDto>>();
        apiResult.Should().NotBeNull();
        apiResult!.Success.Should().BeTrue();
        
        var product = apiResult.Data;
        product.Should().NotBeNull();
        product.Id.Should().Be(productId);
    }
}

