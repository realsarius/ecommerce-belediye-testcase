using System.Net;
using System.Net.Http.Json;
using EcommerceAPI.Entities.DTOs;
using EcommerceAPI.IntegrationTests.Utilities;
using FluentAssertions;
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
        var client = _factory.CreateClient().AsAnonymous();
        var request = new CheckoutRequest
        {
            ShippingAddress = "Test Address",
            PaymentMethod = "CreditCard"
        };

        var response = await client.PostAsJsonAsync("/api/v1/orders", request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Checkout_EmptyCart_ReturnsBadRequest()
    {
        var client = _factory.CreateClient().AsCustomer(userId: 10);
        await client.DeleteAsync("/api/v1/cart");
        
        var request = new CheckoutRequest
        {
            ShippingAddress = "Test Address 123",
            PaymentMethod = "CreditCard"
        };

        var response = await client.PostAsJsonAsync("/api/v1/orders", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetOrders_AuthenticatedUser_ReturnsOrderList()
    {
        var client = _factory.CreateClient().AsCustomer(userId: 11);

        var response = await client.GetAsync("/api/v1/orders");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var apiResult = await response.Content.ReadFromJsonAsync<ApiResult<List<OrderDto>>>();
        apiResult.Should().NotBeNull();
        apiResult!.Success.Should().BeTrue();
        apiResult.Data.Should().NotBeNull();
    }

    [Fact]
    public async Task GetOrderById_NonExistingOrder_ReturnsBadRequest()
    {
        var client = _factory.CreateClient().AsCustomer(userId: 12);
        var nonExistingOrderId = 999999;

        var response = await client.GetAsync($"/api/v1/orders/{nonExistingOrderId}");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CancelOrder_NonExistingOrder_ReturnsBadRequest()
    {
        var client = _factory.CreateClient().AsCustomer(userId: 13);
        var nonExistingOrderId = 999999;
        var requestBody = new UpdateOrderStatusRequest { Status = "Cancelled" };

        var response = await client.PatchAsJsonAsync($"/api/v1/orders/{nonExistingOrderId}/status", requestBody);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact(Skip = "Requires full data seeding for complete checkout flow")]
    public async Task Checkout_ValidCart_CreatesOrder()
    {
        var client = _factory.CreateClient().AsCustomer(userId: 20);
        
        await client.PostAsJsonAsync("/api/v1/cart/items", new AddToCartRequest { ProductId = 1, Quantity = 1 });
        
        var request = new CheckoutRequest
        {
            ShippingAddress = "Test Address 123, Istanbul",
            PaymentMethod = "CreditCard"
        };

        var response = await client.PostAsJsonAsync("/api/v1/orders", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var apiResult = await response.Content.ReadFromJsonAsync<ApiResult<OrderDto>>();
        apiResult.Should().NotBeNull();
        apiResult!.Success.Should().BeTrue();
        
        var order = apiResult.Data;
        order.Should().NotBeNull();
        order.Status.Should().Be("PendingPayment");
    }

}
