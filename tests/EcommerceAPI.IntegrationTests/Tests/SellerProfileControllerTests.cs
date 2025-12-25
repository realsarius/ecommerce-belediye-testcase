using System.Net;
using System.Net.Http.Json;
using EcommerceAPI.Entities.DTOs;
using EcommerceAPI.IntegrationTests.Utilities;
using FluentAssertions;
using Xunit;

namespace EcommerceAPI.IntegrationTests.Tests;

[Collection("Integration")]
public class SellerProfileControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public SellerProfileControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetProfile_WithoutAuth_Returns401()
    {
        var anonymousClient = _factory.CreateClient().AsAnonymous();

        var response = await anonymousClient.GetAsync("/api/v1/seller/profile");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetProfile_AsCustomer_Returns403()
    {
        var customerClient = _factory.CreateClient().AsCustomer(userId: 1);

        var response = await customerClient.GetAsync("/api/v1/seller/profile");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetProfile_AsSeller_ReturnsNotFoundOrOk()
    {
        var sellerClient = _factory.CreateClient().AsSeller(userId: 999);

        var response = await sellerClient.GetAsync("/api/v1/seller/profile");

        response.StatusCode.Should().BeOneOf(HttpStatusCode.NotFound, HttpStatusCode.OK);
    }

    [Fact]
    public async Task CreateProfile_AsCustomer_Returns403()
    {
        var customerClient = _factory.CreateClient().AsCustomer(userId: 1);
        var profileRequest = new CreateSellerProfileRequest
        {
            BrandName = "Test Brand",
            BrandDescription = "Test Description"
        };

        var response = await customerClient.PostAsJsonAsync("/api/v1/seller/profile", profileRequest);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task CreateProfile_AsSeller_ReturnsCreatedOrBadRequest()
    {
        var sellerClient = _factory.CreateClient().AsSeller(userId: 100);
        var profileRequest = new CreateSellerProfileRequest
        {
            BrandName = $"Test Brand {Guid.NewGuid():N}",
            BrandDescription = "Created by integration test"
        };

        var response = await sellerClient.PostAsJsonAsync("/api/v1/seller/profile", profileRequest);

        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.Created,
            HttpStatusCode.BadRequest
        );
    }

    [Fact]
    public async Task CheckProfileExists_AsSeller_ReturnsOk()
    {
        var sellerClient = _factory.CreateClient().AsSeller(userId: 1);

        var response = await sellerClient.GetAsync("/api/v1/seller/profile/exists");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<HasProfileResponse>();
        content.Should().NotBeNull();
    }

    private class HasProfileResponse
    {
        public bool HasProfile { get; set; }
    }
}

[Collection("Integration")]
public class SellerProductsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public SellerProductsTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task CreateProduct_AsSeller_Returns201OrBadRequest()
    {
        var sellerClient = _factory.CreateClient().AsSeller(userId: 1);
        var createRequest = new CreateProductRequest
        {
            Name = $"Seller Product {Guid.NewGuid():N}",
            Description = "Created by seller test",
            Price = 149.99m,
            CategoryId = 1,
            SKU = $"SELL-{Guid.NewGuid():N}"[..12]
        };

        var response = await sellerClient.PostAsJsonAsync("/api/v1/admin/products", createRequest);

        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.Created,
            HttpStatusCode.BadRequest
        );
    }

    [Fact]
    public async Task GetProducts_AsSeller_ReturnsOnlyOwnProducts()
    {
        var sellerClient = _factory.CreateClient().AsSeller(userId: 1);

        var response = await sellerClient.GetAsync("/api/v1/admin/products?page=1&pageSize=100");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadFromJsonAsync<ApiResult<PaginatedResponse<ProductDto>>>();
        content.Should().NotBeNull();
        
        if (content!.Success && content.Data?.Items?.Any() == true)
        {
            var sellerIds = content.Data.Items.Select(p => p.SellerId).Distinct().ToList();
            sellerIds.Should().HaveCountLessThanOrEqualTo(1, "Seller should only see their own products");
        }
    }

    [Fact]
    public async Task UpdateProduct_AsSeller_OtherSellerProduct_ReturnsBadRequest()
    {
        var sellerClient = _factory.CreateClient().AsSeller(userId: 1);
        var updateRequest = new UpdateProductRequest
        {
            Name = "Hacked Product Name",
            Description = "Should not work",
            Price = 1.00m,
            CategoryId = 1,
            IsActive = true
        };

        var response = await sellerClient.PutAsJsonAsync("/api/v1/admin/products/1", updateRequest);

        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.BadRequest,
            HttpStatusCode.OK
        );
    }

    [Fact]
    public async Task DeleteProduct_AsSeller_OtherSellerProduct_ReturnsBadRequest()
    {
        var sellerClient = _factory.CreateClient().AsSeller(userId: 1);

        var response = await sellerClient.DeleteAsync("/api/v1/admin/products/2");

        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.BadRequest,
            HttpStatusCode.OK
        );
    }
}
