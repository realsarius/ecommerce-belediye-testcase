using System.Net;
using System.Net.Http.Json;
using EcommerceAPI.DataAccess.Concrete.EntityFramework.Contexts;
using EcommerceAPI.Entities.DTOs;
using EcommerceAPI.Entities.Enums;
using EcommerceAPI.IntegrationTests.Utilities;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EcommerceAPI.IntegrationTests.Tests;

[Collection("Integration")]
public class SellerOrdersControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public SellerOrdersControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task ShipOrder_AsSellerForOwnOrder_ShouldUpdateShipmentFields()
    {
        var customerUserId = Random.Shared.Next(897_001, 898_000);
        var sellerUserId = Random.Shared.Next(898_001, 899_000);
        var categoryId = Random.Shared.Next(899_001, 900_000);
        var productId = Random.Shared.Next(900_001, 901_000);
        var orderId = Random.Shared.Next(901_001, 902_000);
        var estimatedDeliveryDate = DateTime.UtcNow.Date.AddDays(2);

        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var sellerProfile = await TestDataSeeder.EnsureSellerProfileAsync(db, sellerUserId);

            await TestDataSeeder.EnsureOrderWithPaymentAsync(
                db,
                orderId,
                customerUserId,
                productId,
                categoryId,
                $"SHIP-{orderId}",
                $"payment-ship-{orderId}",
                sellerId: sellerProfile.Id,
                orderStatus: OrderStatus.Paid,
                paymentStatus: PaymentStatus.Success);
        }

        var sellerClient = _factory.CreateClient().AsSeller(sellerUserId);
        var response = await sellerClient.PutAsJsonAsync($"/api/v1/seller/orders/{orderId}/ship", new ShipOrderRequest
        {
            CargoProvider = CargoProvider.YurticiKargo,
            TrackingCode = "YT123456789",
            EstimatedDeliveryDate = estimatedDeliveryDate
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ApiResult<OrderDto>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Data.Id.Should().Be(orderId);
        result.Data.Status.Should().Be(OrderStatus.Shipped.ToString());
        result.Data.CargoCompany.Should().Be("Yurtiçi Kargo");
        result.Data.TrackingCode.Should().Be("YT123456789");
        result.Data.ShipmentStatus.Should().Be(ShipmentStatus.HandedToCargo.ToString());

        await using var assertScope = _factory.Services.CreateAsyncScope();
        var assertDb = assertScope.ServiceProvider.GetRequiredService<AppDbContext>();
        var order = await assertDb.Orders.SingleAsync(o => o.Id == orderId);

        order.Status.Should().Be(OrderStatus.Shipped);
        order.CargoCompany.Should().Be("Yurtiçi Kargo");
        order.TrackingCode.Should().Be("YT123456789");
        order.ShipmentStatus.Should().Be(ShipmentStatus.HandedToCargo);
        order.EstimatedDeliveryDate.Should().Be(estimatedDeliveryDate);
        order.ShippedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task ShipOrder_WhenOrderStatusIsNotShippable_ShouldReturnBadRequest()
    {
        var customerUserId = Random.Shared.Next(902_001, 903_000);
        var sellerUserId = Random.Shared.Next(903_001, 904_000);
        var categoryId = Random.Shared.Next(904_001, 905_000);
        var productId = Random.Shared.Next(905_001, 906_000);
        var orderId = Random.Shared.Next(906_001, 907_000);

        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var sellerProfile = await TestDataSeeder.EnsureSellerProfileAsync(db, sellerUserId);

            await TestDataSeeder.EnsureOrderWithPaymentAsync(
                db,
                orderId,
                customerUserId,
                productId,
                categoryId,
                $"SHIP-BLOCKED-{orderId}",
                $"payment-ship-blocked-{orderId}",
                sellerId: sellerProfile.Id,
                orderStatus: OrderStatus.Delivered,
                paymentStatus: PaymentStatus.Success);
        }

        var sellerClient = _factory.CreateClient().AsSeller(sellerUserId);
        var response = await sellerClient.PutAsJsonAsync($"/api/v1/seller/orders/{orderId}/ship", new ShipOrderRequest
        {
            CargoProvider = CargoProvider.YurticiKargo,
            TrackingCode = "YT987654321"
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var result = await response.Content.ReadFromJsonAsync<ApiResult<OrderDto>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeFalse();
        result.Message.Should().Contain("ödenmiş veya hazırlanmakta");
    }
}
