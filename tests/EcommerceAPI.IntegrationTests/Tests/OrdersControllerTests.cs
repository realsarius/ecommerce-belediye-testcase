using System.Net;
using System.Net.Http.Json;
using EcommerceAPI.Entities.DTOs;
using EcommerceAPI.IntegrationTests.Utilities;
using FluentAssertions;
using EcommerceAPI.DataAccess.Concrete.EntityFramework.Contexts;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EcommerceAPI.IntegrationTests.Tests;

[Collection("Integration")]
public class OrdersControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public OrdersControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Checkout_WithoutAuth_Returns401()
    {
        var anonymousClient = _factory.CreateClient().AsAnonymous();
        var checkoutRequest = new CheckoutRequest
        {
            ShippingAddress = "Test Address",
            PaymentMethod = "CreditCard"
        };

        var response = await anonymousClient.PostAsJsonAsync("/api/v1/orders", checkoutRequest);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Checkout_EmptyCart_ReturnsBadRequest()
    {
        var authenticatedClient = _factory.CreateClient().AsCustomer(userId: 10);
        
        await authenticatedClient.DeleteAsync("/api/v1/cart");
        
        var checkoutRequest = new CheckoutRequest
        {
            ShippingAddress = "Test Address 123",
            PaymentMethod = "CreditCard"
        };

        var response = await authenticatedClient.PostAsJsonAsync("/api/v1/orders", checkoutRequest);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetOrders_AuthenticatedUser_ReturnsOrderList()
    {
        var authenticatedClient = _factory.CreateClient().AsCustomer(userId: 11);

        var response = await authenticatedClient.GetAsync("/api/v1/orders");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var apiResult = await response.Content.ReadFromJsonAsync<ApiResult<List<OrderDto>>>();
        apiResult.Should().NotBeNull();
        apiResult!.Success.Should().BeTrue();
        apiResult.Data.Should().NotBeNull();
    }

    [Fact]
    public async Task GetOrderById_NonExistingOrder_ReturnsBadRequest()
    {
        var authenticatedClient = _factory.CreateClient().AsCustomer(userId: 12);
        var nonExistingOrderId = 999999;

        var response = await authenticatedClient.GetAsync($"/api/v1/orders/{nonExistingOrderId}");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CancelOrder_NonExistingOrder_ReturnsBadRequest()
    {
        var authenticatedClient = _factory.CreateClient().AsCustomer(userId: 13);
        var nonExistingOrderId = 999999;

        var cancelRequest = new UpdateOrderStatusRequest { Status = "Cancelled" };
        var response = await authenticatedClient.PatchAsJsonAsync($"/api/v1/orders/{nonExistingOrderId}/status", cancelRequest);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact(Skip = "Requires full data seeding for complete checkout flow")]
    public async Task Checkout_ValidCart_CreatesOrder()
    {
        var authenticatedClient = _factory.CreateClient().AsCustomer(userId: 20);
        
        await authenticatedClient.PostAsJsonAsync("/api/v1/cart/items", new AddToCartRequest { ProductId = 1, Quantity = 1 });
        
        var checkoutRequest = new CheckoutRequest
        {
            ShippingAddress = "Test Address 123, Istanbul",
            PaymentMethod = "CreditCard"
        };

        var response = await authenticatedClient.PostAsJsonAsync("/api/v1/orders", checkoutRequest);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var apiResult = await response.Content.ReadFromJsonAsync<ApiResult<OrderDto>>();
        apiResult.Should().NotBeNull();
        apiResult!.Success.Should().BeTrue();
        
        var order = apiResult.Data;
        order.Should().NotBeNull();
        order.Status.Should().Be("PendingPayment");
    }

    [Fact]
    public async Task Checkout_WithSameIdempotencyKeyHeader_ShouldReturnExistingOrder()
    {
        var userId = Random.Shared.Next(820_001, 830_000);
        var categoryId = Random.Shared.Next(830_001, 840_000);
        var productId = Random.Shared.Next(840_001, 850_000);

        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await TestDataSeeder.EnsureUserAsync(db, userId);
            await TestDataSeeder.EnsureCategoryAsync(db, categoryId, $"Order Category {categoryId}");
            await TestDataSeeder.EnsureProductWithStockAsync(db, productId, categoryId, 10);
        }

        var client = _factory.CreateClient().AsCustomer(userId);
        var addToCartResponse = await client.PostAsJsonAsync("/api/v1/cart/items", new AddToCartRequest
        {
            ProductId = productId,
            Quantity = 1
        });

        addToCartResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        const string idempotencyKey = "integration-checkout-idempotency";
        client.DefaultRequestHeaders.Remove("Idempotency-Key");
        client.DefaultRequestHeaders.Add("Idempotency-Key", idempotencyKey);

        var request = new CheckoutRequest
        {
            ShippingAddress = "Test Address 123, Istanbul",
            PaymentMethod = "CreditCard"
        };

        var firstResponse = await client.PostAsJsonAsync("/api/v1/orders", request);
        var secondResponse = await client.PostAsJsonAsync("/api/v1/orders", request);

        firstResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        secondResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var firstResult = await firstResponse.Content.ReadFromJsonAsync<ApiResult<OrderDto>>();
        var secondResult = await secondResponse.Content.ReadFromJsonAsync<ApiResult<OrderDto>>();

        firstResult.Should().NotBeNull();
        secondResult.Should().NotBeNull();
        firstResult!.Success.Should().BeTrue();
        secondResult!.Success.Should().BeTrue();
        secondResult.Data.Id.Should().Be(firstResult.Data.Id);
        secondResult.Message.Should().Contain("Idempotent");
    }
}
