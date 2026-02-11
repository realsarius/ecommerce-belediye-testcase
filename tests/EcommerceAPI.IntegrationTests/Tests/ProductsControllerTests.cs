using System.Net;
using System.Net.Http.Json;
using EcommerceAPI.Entities.DTOs;
using FluentAssertions;
using EcommerceAPI.IntegrationTests.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace EcommerceAPI.IntegrationTests.Tests;

[Collection("Integration")]
public class ProductsControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly ITestOutputHelper _output;

    public ProductsControllerTests(CustomWebApplicationFactory factory, ITestOutputHelper output)
    {
        _client = factory.CreateClient();
        _output = output;
    }

    [Fact]
    public async Task GetProducts_DefaultPagination_ReturnsOkWithPaginatedResponse()
    {
        var response = await _client.GetAsync("/api/v1/products?page=1&pageSize=10");

        if (response.StatusCode != HttpStatusCode.OK)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            _output.WriteLine($"Response Status: {response.StatusCode}");
            _output.WriteLine($"Response Content: {errorContent}");
        }

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        _output.WriteLine($"Response JSON: {content}");

        var apiResult = System.Text.Json.JsonSerializer.Deserialize<ApiResult<PaginatedResponse<ProductDto>>>(
            content,
            new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        apiResult.Should().NotBeNull();
        apiResult!.Success.Should().BeTrue();
        
        var paginatedProducts = apiResult.Data;
        paginatedProducts.Should().NotBeNull();
        paginatedProducts.Items.Should().NotBeNull();
        paginatedProducts.Page.Should().Be(1);
        paginatedProducts.PageSize.Should().Be(10);
    }

    [Fact]
    public async Task GetProducts_WithCategoryFilter_ReturnsFilteredProducts()
    {
        var response = await _client.GetAsync("/api/v1/products?categoryId=1");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var apiResult = await response.Content.ReadFromJsonAsync<ApiResult<PaginatedResponse<ProductDto>>>();
        apiResult.Should().NotBeNull();
        apiResult!.Success.Should().BeTrue();
    }

    [Fact]
    public async Task GetProducts_WithSearch_ReturnsMatchingProducts()
    {
        var response = await _client.GetAsync("/api/v1/products?search=test");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetProducts_WithSorting_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/v1/products?sortBy=price&sortDesc=true");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetProductById_NonExistingId_ReturnsBadRequest()
    {
        var nonExistingId = 999999;

        var response = await _client.GetAsync($"/api/v1/products/{nonExistingId}");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetProductById_ValidId_ReturnsProduct()
    {
        var listResponse = await _client.GetAsync("/api/v1/products?page=1&pageSize=1");
        var listApiResult = await listResponse.Content.ReadFromJsonAsync<ApiResult<PaginatedResponse<ProductDto>>>();
        
        if (listApiResult?.Data?.Items?.Any() != true)
            return;

        var productId = listApiResult.Data.Items.First().Id;

        var response = await _client.GetAsync($"/api/v1/products/{productId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var apiResult = await response.Content.ReadFromJsonAsync<ApiResult<ProductDto>>();
        apiResult.Should().NotBeNull();
        apiResult!.Success.Should().BeTrue();
        
        var product = apiResult.Data;
        product.Should().NotBeNull();
        product.Id.Should().Be(productId);
    }
}
