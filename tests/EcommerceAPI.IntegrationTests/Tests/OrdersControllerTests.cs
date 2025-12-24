using System.Net;
using System.Net.Http.Json;
using EcommerceAPI.Entities.DTOs;
using EcommerceAPI.IntegrationTests.Utilities;
using FluentAssertions;
using Xunit;

namespace EcommerceAPI.IntegrationTests.Tests;

// Orders controller tests - checkout, listing, cancellation

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
        // Arrange
        var client = _factory.CreateClient().AsAnonymous();
        var request = new CheckoutRequest
        {
            ShippingAddress = "Test Address",
            PaymentMethod = "CreditCard"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/orders/checkout", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Checkout_EmptyCart_ReturnsBadRequest()
    {
        // Arrange
        var client = _factory.CreateClient().AsCustomer(userId: 10);
        
        // Clear cart first to ensure it's empty
        await client.DeleteAsync("/api/v1/cart");
        
        var request = new CheckoutRequest
        {
            ShippingAddress = "Test Address 123",
            PaymentMethod = "CreditCard"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/orders/checkout", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetOrders_AuthenticatedUser_ReturnsOrderList()
    {
        // Arrange
        var client = _factory.CreateClient().AsCustomer(userId: 11);

        // Act
        var response = await client.GetAsync("/api/v1/orders");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var apiResult = await response.Content.ReadFromJsonAsync<ApiResult<List<OrderDto>>>();
        apiResult.Should().NotBeNull();
        apiResult!.Success.Should().BeTrue();
        apiResult.Data.Should().NotBeNull();
    }

    [Fact]
    public async Task GetOrderById_NonExistingOrder_ReturnsBadRequest()
    {
        // Arrange
        var client = _factory.CreateClient().AsCustomer(userId: 12);
        var nonExistingOrderId = 999999;

        // Act
        var response = await client.GetAsync($"/api/v1/orders/{nonExistingOrderId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CancelOrder_NonExistingOrder_ReturnsBadRequest()
    {
        // Arrange
        var client = _factory.CreateClient().AsCustomer(userId: 13);
        var nonExistingOrderId = 999999;

        // Act
        var response = await client.PostAsync($"/api/v1/orders/{nonExistingOrderId}/cancel", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // Note: Full checkout flow test requires seeded products and careful setup
    // This is marked as skipped until proper seeding is in place
    [Fact(Skip = "Requires full data seeding for complete checkout flow")]
    public async Task Checkout_ValidCart_CreatesOrder()
    {
        // Arrange
        var client = _factory.CreateClient().AsCustomer(userId: 20);
        
        // Add product to cart
        await client.PostAsJsonAsync("/api/v1/cart/items", new AddToCartRequest { ProductId = 1, Quantity = 1 });
        
        var request = new CheckoutRequest
        {
            ShippingAddress = "Test Address 123, Istanbul",
            PaymentMethod = "CreditCard"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/orders/checkout", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var apiResult = await response.Content.ReadFromJsonAsync<ApiResult<OrderDto>>();
        apiResult.Should().NotBeNull();
        apiResult!.Success.Should().BeTrue();
        
        var order = apiResult.Data;
        order.Should().NotBeNull();
        order.Status.Should().Be("PendingPayment");
    }

}
