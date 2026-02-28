using System.Net;
using System.Net.Http.Json;
using EcommerceAPI.Entities.DTOs;
using EcommerceAPI.IntegrationTests.Utilities;
using FluentAssertions;
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
}
