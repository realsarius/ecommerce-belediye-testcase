using System.Net;
using System.Net.Http.Json;
using EcommerceAPI.DataAccess.Concrete.EntityFramework.Contexts;
using EcommerceAPI.Entities.DTOs;
using EcommerceAPI.Entities.Enums;
using EcommerceAPI.IntegrationTests.Utilities;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace EcommerceAPI.IntegrationTests.Tests;

[Collection("Integration")]
public class ReturnsControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public ReturnsControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task CreateReturnRequest_ForDeliveredOrder_ReturnsOk()
    {
        var userId = Random.Shared.Next(880_001, 881_000);
        var categoryId = Random.Shared.Next(881_001, 882_000);
        var productId = Random.Shared.Next(882_001, 883_000);
        var orderId = Random.Shared.Next(883_001, 884_000);

        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await TestDataSeeder.EnsureOrderWithPaymentAsync(
                db,
                orderId,
                userId,
                productId,
                categoryId,
                $"RET-{orderId}",
                $"payment-return-{orderId}",
                orderStatus: OrderStatus.Delivered,
                paymentStatus: PaymentStatus.Success);
        }

        var client = _factory.CreateClient().AsCustomer(userId);
        var response = await client.PostAsJsonAsync($"/api/v1/orders/{orderId}/returns", new CreateReturnRequestRequest
        {
            Type = "Return",
            Reason = "Ürün beklentimi karşılamadı",
            RequestNote = "Paket açıldı ancak kullanılmadı."
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ApiResult<ReturnRequestDto>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Data.OrderId.Should().Be(orderId);
        result.Data.Status.Should().Be(ReturnRequestStatus.Pending.ToString());
        result.Data.Type.Should().Be(ReturnRequestType.Return.ToString());
    }

    [Fact]
    public async Task ReviewReturnRequest_AsSellerForOwnOrder_ReturnsRefundPending()
    {
        var customerUserId = Random.Shared.Next(884_001, 885_000);
        var sellerUserId = Random.Shared.Next(885_001, 886_000);
        var categoryId = Random.Shared.Next(886_001, 887_000);
        var productId = Random.Shared.Next(887_001, 888_000);
        var orderId = Random.Shared.Next(888_001, 889_000);

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
                $"RET-SELLER-{orderId}",
                $"payment-review-{orderId}",
                sellerId: sellerProfile.Id,
                orderStatus: OrderStatus.Delivered,
                paymentStatus: PaymentStatus.Success);
        }

        var customerClient = _factory.CreateClient().AsCustomer(customerUserId);
        var createResponse = await customerClient.PostAsJsonAsync($"/api/v1/orders/{orderId}/returns", new CreateReturnRequestRequest
        {
            Type = "Return",
            Reason = "Ürünü iade etmek istiyorum"
        });

        createResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var createResult = await createResponse.Content.ReadFromJsonAsync<ApiResult<ReturnRequestDto>>();
        createResult.Should().NotBeNull();

        var sellerClient = _factory.CreateClient().AsSeller(sellerUserId);
        var reviewResponse = await sellerClient.PatchAsJsonAsync($"/api/v1/admin/returns/{createResult!.Data.Id}", new ReviewReturnRequestRequest
        {
            Status = "Approved",
            ReviewNote = "Talep onaylandı"
        });

        reviewResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var reviewResult = await reviewResponse.Content.ReadFromJsonAsync<ApiResult<ReturnRequestDto>>();
        reviewResult.Should().NotBeNull();
        reviewResult!.Success.Should().BeTrue();
        reviewResult.Data.Status.Should().Be(ReturnRequestStatus.RefundPending.ToString());
        reviewResult.Data.RefundStatus.Should().Be(RefundRequestStatus.Pending.ToString());
    }
}
