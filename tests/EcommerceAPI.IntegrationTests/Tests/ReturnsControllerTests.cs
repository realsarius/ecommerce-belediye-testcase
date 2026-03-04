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
            ReasonCategory = "NotAsDescribed",
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
        result.Data.ReasonCategory.Should().Be(ReturnReasonCategory.NotAsDescribed.ToString());
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
            ReasonCategory = "WrongProduct",
            Reason = "Ürünü iade etmek istiyorum"
        });

        createResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var createResult = await createResponse.Content.ReadFromJsonAsync<ApiResult<ReturnRequestDto>>();
        createResult.Should().NotBeNull();

        var sellerClient = _factory.CreateClient().AsSeller(sellerUserId);
        var reviewResponse = await sellerClient.PutAsJsonAsync($"/api/v1/seller/returns/{createResult!.Data.Id}/approve", new ReviewReturnRequestRequest
        {
            ReviewNote = "Talep onaylandı"
        });

        reviewResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var reviewResult = await reviewResponse.Content.ReadFromJsonAsync<ApiResult<ReturnRequestDto>>();
        reviewResult.Should().NotBeNull();
        reviewResult!.Success.Should().BeTrue();
        reviewResult.Data.Status.Should().Be(ReturnRequestStatus.RefundPending.ToString());
        reviewResult.Data.RefundStatus.Should().Be(RefundRequestStatus.Pending.ToString());
    }

    [Fact]
    public async Task ReviewReturnRequest_AsAdminApprove_ShouldCreateRefundRequest()
    {
        const int adminUserId = 999;
        var customerUserId = Random.Shared.Next(889_001, 890_000);
        var categoryId = Random.Shared.Next(890_001, 891_000);
        var productId = Random.Shared.Next(891_001, 892_000);
        var orderId = Random.Shared.Next(892_001, 893_000);

        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await TestDataSeeder.EnsureUserAsync(db, adminUserId, "Admin");

            await TestDataSeeder.EnsureOrderWithPaymentAsync(
                db,
                orderId,
                customerUserId,
                productId,
                categoryId,
                $"RET-ADMIN-{orderId}",
                $"payment-admin-review-{orderId}",
                orderStatus: OrderStatus.Delivered,
                paymentStatus: PaymentStatus.Success);
        }

        var customerClient = _factory.CreateClient().AsCustomer(customerUserId);
        var createResponse = await customerClient.PostAsJsonAsync($"/api/v1/orders/{orderId}/returns", new CreateReturnRequestRequest
        {
            Type = "Return",
            ReasonCategory = "DefectiveDamaged",
            Reason = "Ürün hasarlı geldi"
        });

        createResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var createResult = await createResponse.Content.ReadFromJsonAsync<ApiResult<ReturnRequestDto>>();
        createResult.Should().NotBeNull();

        var adminClient = _factory.CreateClient().AsAdmin(userId: adminUserId);
        var reviewResponse = await adminClient.PutAsJsonAsync($"/api/v1/admin/returns/{createResult!.Data.Id}/approve", new ReviewReturnRequestRequest
        {
            ReviewNote = "Hasar doğrulandı, refund başlatıldı"
        });

        reviewResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var reviewResult = await reviewResponse.Content.ReadFromJsonAsync<ApiResult<ReturnRequestDto>>();
        reviewResult.Should().NotBeNull();
        reviewResult!.Success.Should().BeTrue();
        reviewResult.Data.Status.Should().Be(ReturnRequestStatus.RefundPending.ToString());
        reviewResult.Data.RefundStatus.Should().Be(RefundRequestStatus.Pending.ToString());
        reviewResult.Data.ReviewNote.Should().Be("Hasar doğrulandı, refund başlatıldı");

        await using var assertScope = _factory.Services.CreateAsyncScope();
        var assertDb = assertScope.ServiceProvider.GetRequiredService<AppDbContext>();
        var refundRequest = await assertDb.RefundRequests
            .SingleAsync(rr => rr.ReturnRequestId == createResult.Data.Id);

        refundRequest.OrderId.Should().Be(orderId);
        refundRequest.Status.Should().Be(RefundRequestStatus.Pending);
        refundRequest.Amount.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task ReviewReturnRequest_AsAdminReject_ShouldExposeReviewNoteToCustomer()
    {
        const int adminUserId = 1001;
        var customerUserId = Random.Shared.Next(893_001, 894_000);
        var categoryId = Random.Shared.Next(894_001, 895_000);
        var productId = Random.Shared.Next(895_001, 896_000);
        var orderId = Random.Shared.Next(896_001, 897_000);

        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await TestDataSeeder.EnsureUserAsync(db, adminUserId, "Admin");

            await TestDataSeeder.EnsureOrderWithPaymentAsync(
                db,
                orderId,
                customerUserId,
                productId,
                categoryId,
                $"RET-REJECT-{orderId}",
                $"payment-admin-reject-{orderId}",
                orderStatus: OrderStatus.Delivered,
                paymentStatus: PaymentStatus.Success);
        }

        var customerClient = _factory.CreateClient().AsCustomer(customerUserId);
        var createResponse = await customerClient.PostAsJsonAsync($"/api/v1/orders/{orderId}/returns", new CreateReturnRequestRequest
        {
            Type = "Return",
            ReasonCategory = "ChangedMind",
            Reason = "Vazgeçtim ama koşullar uygun değil"
        });

        createResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var createResult = await createResponse.Content.ReadFromJsonAsync<ApiResult<ReturnRequestDto>>();
        createResult.Should().NotBeNull();

        var adminClient = _factory.CreateClient().AsAdmin(userId: adminUserId);
        var reviewResponse = await adminClient.PutAsJsonAsync($"/api/v1/admin/returns/{createResult!.Data.Id}/reject", new ReviewReturnRequestRequest
        {
            ReviewNote = "Ürün hijyen kategorisinde olduğu için iade kapsamı dışında."
        });

        reviewResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var reviewResult = await reviewResponse.Content.ReadFromJsonAsync<ApiResult<ReturnRequestDto>>();
        reviewResult.Should().NotBeNull();
        reviewResult!.Success.Should().BeTrue();
        reviewResult.Data.Status.Should().Be(ReturnRequestStatus.Rejected.ToString());
        reviewResult.Data.ReviewNote.Should().Be("Ürün hijyen kategorisinde olduğu için iade kapsamı dışında.");

        var customerReturnsResponse = await customerClient.GetAsync("/api/v1/returns/mine");
        customerReturnsResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var customerReturns = await customerReturnsResponse.Content.ReadFromJsonAsync<ApiResult<List<ReturnRequestDto>>>();
        customerReturns.Should().NotBeNull();
        customerReturns!.Success.Should().BeTrue();

        var rejectedRequest = customerReturns.Data.Single(r => r.Id == createResult.Data.Id);
        rejectedRequest.Status.Should().Be(ReturnRequestStatus.Rejected.ToString());
        rejectedRequest.ReviewNote.Should().Be("Ürün hijyen kategorisinde olduğu için iade kapsamı dışında.");
    }
}
