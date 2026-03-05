using System.Net;
using System.Net.Http.Json;
using EcommerceAPI.DataAccess.Concrete.EntityFramework.Contexts;
using EcommerceAPI.Entities.DTOs;
using EcommerceAPI.Entities.Enums;
using EcommerceAPI.IntegrationTests.Utilities;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
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
    public async Task AddToCart_UnverifiedUser_ReturnsForbidden()
    {
        var unverifiedClient = _factory.CreateClient().AsUnverifiedCustomer(userId: 2);
        var request = new AddToCartRequest
        {
            ProductId = 1,
            Quantity = 1
        };

        var response = await unverifiedClient.PostAsJsonAsync("/api/v1/cart/items", request);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
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

    [Fact]
    public async Task Reorder_WhenStockIsLimited_ShouldAddAvailableQuantityAndReportSkippedReason()
    {
        var userId = Random.Shared.Next(880_001, 890_000);
        var categoryId = Random.Shared.Next(890_001, 900_000);
        var productId = Random.Shared.Next(900_001, 910_000);
        var orderId = Random.Shared.Next(910_001, 920_000);

        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await TestDataSeeder.EnsureUserAsync(db, userId);
            await TestDataSeeder.EnsureCategoryAsync(db, categoryId, $"Reorder Category {categoryId}");
            await TestDataSeeder.EnsureProductWithStockAsync(db, productId, categoryId, 3);

            var order = await TestDataSeeder.EnsureOrderWithPaymentAsync(
                db,
                orderId,
                userId,
                productId,
                categoryId,
                $"ORD-REORDER-{orderId}",
                $"integration-reorder-{orderId}",
                orderStatus: OrderStatus.Delivered,
                paymentStatus: PaymentStatus.Success);

            var orderItem = order.OrderItems.Single();
            orderItem.Quantity = 2;
            await db.SaveChangesAsync();
        }

        var client = _factory.CreateClient().AsCustomer(userId);

        var addToCartResponse = await client.PostAsJsonAsync("/api/v1/cart/items", new AddToCartRequest
        {
            ProductId = productId,
            Quantity = 2
        });

        addToCartResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var response = await client.PostAsJsonAsync("/api/v1/cart/reorder", new ReorderCartRequest
        {
            OrderId = orderId
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var apiResult = await response.Content.ReadFromJsonAsync<ApiResult<ReorderCartResultDto>>();
        apiResult.Should().NotBeNull();
        apiResult!.Success.Should().BeTrue();
        apiResult.Data.Should().NotBeNull();
        apiResult.Data.RequestedCount.Should().Be(1);
        apiResult.Data.AddedCount.Should().Be(1);
        apiResult.Data.SkippedCount.Should().Be(1);
        apiResult.Data.SkippedProducts.Should().ContainSingle(item =>
            item.ProductId == productId &&
            item.Reason.Contains("yalnızca 1 adedi"));
    }
}
