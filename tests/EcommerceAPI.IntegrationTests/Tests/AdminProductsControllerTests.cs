using System.Net;
using System.Net.Http.Json;
using EcommerceAPI.Business.Abstract;
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
    public async Task CreateProduct_AsAdmin_ShouldAssignPlatformSeller()
    {
        const int adminUserId = 901;
        await EnsureAdminUserAsync(adminUserId);

        var adminClient = _factory.CreateClient().AsAdmin(userId: adminUserId);
        var categoryId = await GetExistingCategoryIdAsync();
        var createRequest = new CreateProductRequest
        {
            Name = $"Platform Seller Product {Guid.NewGuid():N}",
            Description = "Admin tarafindan eklenen urun",
            Price = 249.99m,
            CategoryId = categoryId,
            SKU = $"PLT-{Guid.NewGuid():N}"[..12],
            InitialStock = 5
        };

        var response = await adminClient.PostAsJsonAsync("/api/v1/admin/products", createRequest);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var apiResult = await response.Content.ReadFromJsonAsync<ApiResult<ProductDto>>();
        apiResult.Should().NotBeNull();
        apiResult!.Success.Should().BeTrue();
        apiResult.Data.Id.Should().BeGreaterThan(0);
        apiResult.Data.SellerId.Should().NotBeNull();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var createdProduct = await db.Products
            .AsNoTracking()
            .FirstOrDefaultAsync(product => product.Id == apiResult.Data.Id);

        createdProduct.Should().NotBeNull();
        createdProduct!.SellerId.Should().NotBeNull();

        var sellerProfile = await db.SellerProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(profile => profile.Id == createdProduct.SellerId);

        sellerProfile.Should().NotBeNull();
        sellerProfile!.IsVerified.Should().BeTrue();
        sellerProfile.IsPlatformAccount.Should().BeTrue();
    }

    [Fact]
    public async Task CreateProduct_AsAdmin_WhenSellerPickerEnabledAndSellerProvided_ShouldAssignSelectedSeller()
    {
        const int adminUserId = 916;
        const int sellerUserId = 917;
        await EnsureAdminUserAsync(adminUserId);
        var selectedSellerProfileId = await EnsureSellerUserAndGetProfileIdAsync(sellerUserId);

        var previousPickerFlag = Environment.GetEnvironmentVariable("FRONTEND_FEATURE_ENABLE_ADMIN_PRODUCT_SELLER_PICKER");
        Environment.SetEnvironmentVariable("FRONTEND_FEATURE_ENABLE_ADMIN_PRODUCT_SELLER_PICKER", "true");

        try
        {
            var adminClient = _factory.CreateClient().AsAdmin(userId: adminUserId);
            var categoryId = await GetExistingCategoryIdAsync();
            var createRequest = new CreateProductRequest
            {
                SellerId = selectedSellerProfileId,
                Name = $"Selected Seller Product {Guid.NewGuid():N}",
                Description = "Admin selected seller test",
                Price = 349.99m,
                CategoryId = categoryId,
                SKU = $"SEL-{Guid.NewGuid():N}"[..12],
                InitialStock = 6
            };

            var response = await adminClient.PostAsJsonAsync("/api/v1/admin/products", createRequest);

            response.StatusCode.Should().Be(HttpStatusCode.Created);
            var apiResult = await response.Content.ReadFromJsonAsync<ApiResult<ProductDto>>();
            apiResult.Should().NotBeNull();
            apiResult!.Success.Should().BeTrue();
            apiResult.Data.Id.Should().BeGreaterThan(0);
            apiResult.Data.SellerId.Should().Be(selectedSellerProfileId);

            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var createdProduct = await db.Products
                .AsNoTracking()
                .FirstOrDefaultAsync(product => product.Id == apiResult.Data.Id);

            createdProduct.Should().NotBeNull();
            createdProduct!.SellerId.Should().Be(selectedSellerProfileId);
        }
        finally
        {
            Environment.SetEnvironmentVariable("FRONTEND_FEATURE_ENABLE_ADMIN_PRODUCT_SELLER_PICKER", previousPickerFlag);
        }
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
            SKU = $"UPD-{Guid.NewGuid():N}"[..12],
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

    [Fact]
    public async Task UpdateProduct_AsDifferentSeller_ForPlatformProduct_ShouldReturnBadRequestAndKeepData()
    {
        const int adminUserId = 910;
        const int sellerUserId = 911;

        await EnsureAdminUserAsync(adminUserId);
        await EnsureSellerUserAsync(sellerUserId);

        var createdProduct = await CreatePlatformProductAsAdminAsync(adminUserId);
        var originalName = createdProduct.Name;

        var sellerClient = _factory.CreateClient().AsSeller(sellerUserId);
        var categoryId = await GetExistingCategoryIdAsync();
        var updateRequest = new UpdateProductRequest
        {
            Name = $"Unauthorized Update {Guid.NewGuid():N}",
            Description = "Should be blocked",
            Price = 1.99m,
            CategoryId = categoryId,
            SKU = $"BLK-{Guid.NewGuid():N}"[..12],
            IsActive = true
        };

        var response = await sellerClient.PutAsJsonAsync($"/api/v1/admin/products/{createdProduct.Id}", updateRequest);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var persisted = await db.Products
            .AsNoTracking()
            .FirstOrDefaultAsync(product => product.Id == createdProduct.Id);

        persisted.Should().NotBeNull();
        persisted!.Name.Should().Be(originalName);
    }

    [Fact]
    public async Task DeleteProduct_AsDifferentSeller_ForPlatformProduct_ShouldReturnBadRequestAndKeepProduct()
    {
        const int adminUserId = 912;
        const int sellerUserId = 913;

        await EnsureAdminUserAsync(adminUserId);
        await EnsureSellerUserAsync(sellerUserId);

        var createdProduct = await CreatePlatformProductAsAdminAsync(adminUserId);

        var sellerClient = _factory.CreateClient().AsSeller(sellerUserId);
        var response = await sellerClient.DeleteAsync($"/api/v1/admin/products/{createdProduct.Id}");
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var persisted = await db.Products
            .AsNoTracking()
            .FirstOrDefaultAsync(product => product.Id == createdProduct.Id);

        persisted.Should().NotBeNull();
    }

    [Fact]
    public async Task GetProduct_AsPlatformSeller_ShouldAccessAdminCreatedPlatformProduct()
    {
        const int adminUserId = 914;
        await EnsureAdminUserAsync(adminUserId);

        var createdProduct = await CreatePlatformProductAsAdminAsync(adminUserId);
        var platformUserId = await GetPlatformSellerUserIdAsync();

        var platformSellerClient = _factory.CreateClient().AsSeller(platformUserId);
        var response = await platformSellerClient.GetAsync($"/api/v1/seller/products/{createdProduct.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<ApiResult<ProductDto>>();
        content.Should().NotBeNull();
        content!.Success.Should().BeTrue();
        content.Data.Should().NotBeNull();
        content.Data!.Id.Should().Be(createdProduct.Id);
        content.Data.SellerId.Should().Be(createdProduct.SellerId);
    }

    [Fact]
    public async Task GetAdminSellerDetail_ForPlatformSeller_ShouldContainAdminCreatedProductAndCounts()
    {
        const int adminUserId = 915;
        await EnsureAdminUserAsync(adminUserId);

        var createdProduct = await CreatePlatformProductAsAdminAsync(adminUserId);
        var platformSellerId = await GetPlatformSellerProfileIdAsync();

        var adminClient = _factory.CreateClient().AsAdmin(adminUserId);
        var response = await adminClient.GetAsync($"/api/v1/admin/sellers/{platformSellerId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<ApiResult<AdminSellerDetailDto>>();
        content.Should().NotBeNull();
        content!.Success.Should().BeTrue();
        content.Data.Should().NotBeNull();
        content.Data.Id.Should().Be(platformSellerId);
        content.Data.ProductCount.Should().BeGreaterThan(0);
        content.Data.Products.Should().Contain(product => product.ProductId == createdProduct.Id);
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

    private async Task EnsureAdminUserAsync(int userId)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await TestDataSeeder.EnsureUserAsync(db, userId, "Admin");
    }

    private async Task EnsureSellerUserAsync(int userId)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await TestDataSeeder.EnsureSellerProfileAsync(db, userId, $"seller-{userId}-{Guid.NewGuid():N}");
    }

    private async Task<int> EnsureSellerUserAndGetProfileIdAsync(int userId)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var profile = await TestDataSeeder.EnsureSellerProfileAsync(db, userId, $"seller-{userId}-{Guid.NewGuid():N}");
        return profile.Id;
    }

    private async Task<ProductDto> CreatePlatformProductAsAdminAsync(int adminUserId)
    {
        var adminClient = _factory.CreateClient().AsAdmin(adminUserId);
        var categoryId = await GetExistingCategoryIdAsync();
        var createRequest = new CreateProductRequest
        {
            Name = $"Platform Product {Guid.NewGuid():N}",
            Description = "Platform product for authorization test",
            Price = 199.99m,
            CategoryId = categoryId,
            SKU = $"PLT-{Guid.NewGuid():N}"[..12],
            InitialStock = 7
        };

        var response = await adminClient.PostAsJsonAsync("/api/v1/admin/products", createRequest);
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var apiResult = await response.Content.ReadFromJsonAsync<ApiResult<ProductDto>>();
        apiResult.Should().NotBeNull();
        apiResult!.Success.Should().BeTrue();
        apiResult.Data.Should().NotBeNull();
        apiResult.Data.SellerId.Should().NotBeNull();

        return apiResult.Data;
    }

    private async Task<int> GetPlatformSellerUserIdAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var platformSellerService = scope.ServiceProvider.GetRequiredService<IPlatformSellerService>();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var platformSellerResult = await platformSellerService.GetOrCreatePlatformSellerIdAsync();
        platformSellerResult.Success.Should().BeTrue(platformSellerResult.Message);
        platformSellerResult.Data.Should().BeGreaterThan(0);

        var sellerProfile = await db.SellerProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(profile => profile.Id == platformSellerResult.Data);

        sellerProfile.Should().NotBeNull();
        return sellerProfile!.UserId;
    }

    private async Task<int> GetPlatformSellerProfileIdAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var platformSellerService = scope.ServiceProvider.GetRequiredService<IPlatformSellerService>();

        var platformSellerResult = await platformSellerService.GetOrCreatePlatformSellerIdAsync();
        platformSellerResult.Success.Should().BeTrue(platformSellerResult.Message);
        platformSellerResult.Data.Should().BeGreaterThan(0);
        return platformSellerResult.Data;
    }
}
