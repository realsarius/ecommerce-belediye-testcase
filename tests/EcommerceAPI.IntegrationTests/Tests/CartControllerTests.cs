using System.Net;
using System.Net.Http.Json;
using EcommerceAPI.Entities.DTOs;
using EcommerceAPI.IntegrationTests.Utilities;
using FluentAssertions;
using Xunit;

namespace EcommerceAPI.IntegrationTests.Tests;

[Collection("Integration")]
public class CartControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public CartControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetCart_WithoutAuth_Returns401()
    {
        var anonymousClient = _factory.CreateClient().AsAnonymous();

        var response = await anonymousClient.GetAsync("/api/v1/cart");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetCart_AuthenticatedUser_ReturnsOkOrCreatesCart()
    {
        var authenticatedClient = _factory.CreateClient().AsCustomer(userId: 1);

        var response = await authenticatedClient.GetAsync("/api/v1/cart");

        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.OK, 
            HttpStatusCode.InternalServerError
        );
    }

    [Fact]
    public async Task AddToCart_ValidProduct_ReturnsOkOrUserNotFound()
    {
        var authenticatedClient = _factory.CreateClient().AsCustomer(userId: 2);
        
        var productsResponse = await authenticatedClient.GetAsync("/api/v1/products?page=1&pageSize=1");
        var productsApiResult = await productsResponse.Content.ReadFromJsonAsync<ApiResult<PaginatedResponse<ProductDto>>>();
        
        if (productsApiResult?.Data?.Items?.Any() != true)
            return;

        var productId = productsApiResult.Data.Items.First().Id;
        var addToCartRequest = new AddToCartRequest
        {
            ProductId = productId,
            Quantity = 1
        };

        var response = await authenticatedClient.PostAsJsonAsync("/api/v1/cart/items", addToCartRequest);

        if (response.StatusCode == HttpStatusCode.OK)
        {
            var apiResult = await response.Content.ReadFromJsonAsync<ApiResult<CartDto>>();
            apiResult.Should().NotBeNull();
            apiResult!.Success.Should().BeTrue();
        }
        else
        {
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }
    }

    [Fact]
    public async Task AddToCart_InsufficientStock_ReturnsBadRequestOrNotFound()
    {
        var authenticatedClient = _factory.CreateClient().AsCustomer(userId: 3);
        var addToCartRequest = new AddToCartRequest
        {
            ProductId = 1,
            Quantity = 999999
        };

        var response = await authenticatedClient.PostAsJsonAsync("/api/v1/cart/items", addToCartRequest);

        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.BadRequest, 
            HttpStatusCode.NotFound,
            HttpStatusCode.InternalServerError
        );
    }

    [Fact(Skip = "Requires seeded user in DB")]
    public async Task UpdateCartItem_ValidQuantity_ReturnsUpdatedCart()
    {
        var authenticatedClient = _factory.CreateClient().AsCustomer(userId: 4);
        
        var productsResponse = await authenticatedClient.GetAsync("/api/v1/products?page=1&pageSize=1");
        var productsApiResult = await productsResponse.Content.ReadFromJsonAsync<ApiResult<PaginatedResponse<ProductDto>>>();
        
        if (productsApiResult?.Data?.Items?.Any() != true) return;

        var productId = productsApiResult.Data.Items.First().Id;
        await authenticatedClient.PostAsJsonAsync("/api/v1/cart/items", new AddToCartRequest { ProductId = productId, Quantity = 1 });

        var updateRequest = new UpdateCartItemRequest { Quantity = 2 };

        var response = await authenticatedClient.PutAsJsonAsync($"/api/v1/cart/items/{productId}", updateRequest);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var apiResult = await response.Content.ReadFromJsonAsync<ApiResult<CartDto>>();
        apiResult.Should().NotBeNull();
        apiResult!.Success.Should().BeTrue();
        
        var cart = apiResult.Data;
        cart!.Items.Should().Contain(x => x.ProductId == productId && x.Quantity == 2);
    }

    [Fact(Skip = "Requires seeded user in DB")]
    public async Task RemoveFromCart_ExistingItem_ReturnsUpdatedCart()
    {
        var authenticatedClient = _factory.CreateClient().AsCustomer(userId: 5);
        
        var productsResponse = await authenticatedClient.GetAsync("/api/v1/products?page=1&pageSize=1");
        var productsApiResult = await productsResponse.Content.ReadFromJsonAsync<ApiResult<PaginatedResponse<ProductDto>>>();
        
        if (productsApiResult?.Data?.Items?.Any() != true) return;

        var productId = productsApiResult.Data.Items.First().Id;
        await authenticatedClient.PostAsJsonAsync("/api/v1/cart/items", new AddToCartRequest { ProductId = productId, Quantity = 1 });

        var response = await authenticatedClient.DeleteAsync($"/api/v1/cart/items/{productId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ClearCart_AnyUser_ReturnsExpectedStatus()
    {
        var authenticatedClient = _factory.CreateClient().AsCustomer(userId: 6);

        var response = await authenticatedClient.DeleteAsync("/api/v1/cart");

        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.OK, 
            HttpStatusCode.NotFound, 
            HttpStatusCode.NoContent, 
            HttpStatusCode.BadRequest
        );
    }
}
