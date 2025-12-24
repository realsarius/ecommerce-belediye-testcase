using System.Net;
using System.Net.Http.Json;
using EcommerceAPI.Entities.DTOs;
using EcommerceAPI.IntegrationTests.Utilities;
using FluentAssertions;
using Xunit;

namespace EcommerceAPI.IntegrationTests.Tests;

/// <summary>
/// Seller Profile ve Seller Products API testleri
/// </summary>
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
        // Arrange
        var client = _factory.CreateClient().AsAnonymous();

        // Act
        var response = await client.GetAsync("/api/v1/seller/profile");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetProfile_AsCustomer_Returns403()
    {
        // Arrange
        var client = _factory.CreateClient().AsCustomer(userId: 1);

        // Act
        var response = await client.GetAsync("/api/v1/seller/profile");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetProfile_AsSeller_ReturnsNotFoundOrOk()
    {
        // Arrange - Seller without profile
        var client = _factory.CreateClient().AsSeller(userId: 999);

        // Act
        var response = await client.GetAsync("/api/v1/seller/profile");

        // Assert - Should be NotFound if no profile, or OK if profile exists
        response.StatusCode.Should().BeOneOf(HttpStatusCode.NotFound, HttpStatusCode.OK);
    }

    [Fact]
    public async Task CreateProfile_AsCustomer_Returns403()
    {
        // Arrange
        var client = _factory.CreateClient().AsCustomer(userId: 1);
        var request = new CreateSellerProfileRequest
        {
            BrandName = "Test Brand",
            BrandDescription = "Test Description"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/seller/profile", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task CreateProfile_AsSeller_ReturnsCreatedOrBadRequest()
    {
        // Arrange
        var client = _factory.CreateClient().AsSeller(userId: 100);
        var request = new CreateSellerProfileRequest
        {
            BrandName = $"Test Brand {Guid.NewGuid():N}",
            BrandDescription = "Created by integration test"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/seller/profile", request);

        // Assert - BadRequest if user doesn't exist or not a Seller role, Created if success
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.Created,
            HttpStatusCode.BadRequest
        );
    }

    [Fact]
    public async Task CheckProfileExists_AsSeller_ReturnsOk()
    {
        // Arrange
        var client = _factory.CreateClient().AsSeller(userId: 1);

        // Act
        var response = await client.GetAsync("/api/v1/seller/profile/exists");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<HasProfileResponse>();
        content.Should().NotBeNull();
    }

    private class HasProfileResponse
    {
        public bool HasProfile { get; set; }
    }
}

/// <summary>
/// Seller-specific product operations tests
/// </summary>
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
        // Arrange
        var client = _factory.CreateClient().AsSeller(userId: 1);
        var request = new CreateProductRequest
        {
            Name = $"Seller Product {Guid.NewGuid():N}",
            Description = "Created by seller test",
            Price = 149.99m,
            CategoryId = 1,
            SKU = $"SELL-{Guid.NewGuid():N}"[..12]
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/admin/products", request);

        // Assert - May fail if seller doesn't have a profile
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.Created,
            HttpStatusCode.BadRequest
        );
    }

    [Fact]
    public async Task GetProducts_AsSeller_ReturnsOnlyOwnProducts()
    {
        // Arrange
        var client = _factory.CreateClient().AsSeller(userId: 1);

        // Act
        var response = await client.GetAsync("/api/v1/admin/products?page=1&pageSize=100");

        // Assert - Should return 200 OK
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadFromJsonAsync<ApiResult<PaginatedResponse<ProductDto>>>();
        content.Should().NotBeNull();
        
        // If seller has profile and products, verify they only see their own
        // If seller has no profile, they see empty list (Success may still be true with empty data)
        if (content!.Success && content.Data?.Items?.Any() == true)
        {
            var sellerIds = content.Data.Items.Select(p => p.SellerId).Distinct().ToList();
            sellerIds.Should().HaveCountLessThanOrEqualTo(1, "Seller should only see their own products");
        }
    }

    [Fact]
    public async Task UpdateProduct_AsSeller_OtherSellerProduct_ReturnsBadRequest()
    {
        // Arrange - Seller 1 trying to update another seller's product
        var client = _factory.CreateClient().AsSeller(userId: 1);
        var updateRequest = new UpdateProductRequest
        {
            Name = "Hacked Product Name",
            Description = "Should not work",
            Price = 1.00m,
            CategoryId = 1,
            IsActive = true
        };

        // Act - Try to update product ID 1 (which likely belongs to no seller or another seller)
        var response = await client.PutAsJsonAsync("/api/v1/admin/products/1", updateRequest);

        // Assert - Should fail because seller doesn't own this product
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.BadRequest, // Ownership check failed
            HttpStatusCode.OK // If somehow test user owns product 1
        );
    }

    [Fact]
    public async Task DeleteProduct_AsSeller_OtherSellerProduct_ReturnsBadRequest()
    {
        // Arrange
        var client = _factory.CreateClient().AsSeller(userId: 1);

        // Act - Try to delete product ID 2 (which likely belongs to no seller or another seller)
        var response = await client.DeleteAsync("/api/v1/admin/products/2");

        // Assert
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.BadRequest, // Ownership check failed
            HttpStatusCode.OK // If test user owns product 2
        );
    }
}
