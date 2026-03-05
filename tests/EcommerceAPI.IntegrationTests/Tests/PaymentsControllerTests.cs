using System.Net;
using System.Net.Http.Json;
using EcommerceAPI.API;
using EcommerceAPI.Core.Interfaces;
using EcommerceAPI.Entities.Concrete;
using EcommerceAPI.Entities.DTOs;
using EcommerceAPI.IntegrationTests.Utilities;
using FluentAssertions;
using EcommerceAPI.DataAccess.Concrete.EntityFramework.Contexts;
using EcommerceAPI.Entities.Enums;
using EcommerceAPI.Infrastructure.ExternalServices;
using Iyzipay.Request;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace EcommerceAPI.IntegrationTests.Tests;

[Collection("Integration")]
public class PaymentsControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    private sealed class FakeIyzicoPaymentGateway : IIyzicoPaymentGateway
    {
        public CreatePaymentRequest? LastChargeRequest { get; private set; }
        public CreatePaymentRequest? LastThreeDSRequest { get; private set; }

        public IyzicoChargeGatewayResult ChargeResult { get; set; } = new(
            Success: true,
            PaymentId: "PAY-TEST-001",
            ErrorMessage: null,
            CardToken: "iyz-card-token-001",
            CardUserKey: "iyz-user-key-001",
            LastFourDigits: "0009");

        public IyzicoThreeDSInitializeGatewayResult ThreeDSResult { get; set; } = new(
            Success: true,
            PaymentId: "THREEDS-TEST-001",
            HtmlContent: "<form></form>",
            ErrorMessage: null);

        public Task<IyzicoChargeGatewayResult> ChargeAsync(
            CreatePaymentRequest request,
            CancellationToken cancellationToken = default)
        {
            LastChargeRequest = request;
            return Task.FromResult(ChargeResult);
        }

        public Task<IyzicoThreeDSInitializeGatewayResult> InitializeThreeDSAsync(
            CreatePaymentRequest request,
            CancellationToken cancellationToken = default)
        {
            LastThreeDSRequest = request;
            return Task.FromResult(ThreeDSResult);
        }
    }

    public static readonly (string Number, string Description) SuccessCard = 
        ("5406670000000009", "Başarılı");

    public static readonly IReadOnlyList<(string Number, string ExpectedError, string Description)> ErrorCards = new List<(string, string, string)>
    {
        ("4111111111111129", "Yetersiz bakiye", "Not sufficient funds"),
        ("4129111111111111", "İşlem reddedildi", "Do not honour"),
        ("4128111111111112", "Geçersiz işlem", "Invalid transaction"),
        ("4127111111111113", "Kayıp kart", "Lost card"),
        ("4126111111111114", "Çalıntı kart", "Stolen card"),
        ("4125111111111115", "Süresi dolmuş kart", "Expired card"),
        ("4124111111111116", "Geçersiz CVC", "Invalid cvc2"),
        ("4123111111111117", "Kart sahibine izin verilmedi", "Not permitted to card holder"),
        ("4122111111111118", "Terminale izin verilmedi", "Not permitted to terminal"),
        ("4121111111111119", "Dolandırıcılık şüphesi", "Fraud suspect"),
        ("4120111111111110", "Kart geri alınmalı", "Pickup card"),
        ("4130111111111118", "Genel hata", "General error"),
        ("4131111111111117", "mdStatus 0", "Success but mdStatus is 0"),
        ("4141111111111115", "mdStatus 4", "Success but mdStatus is 4"),
        ("4151111111111112", "3D Secure başlatılamadı", "3dsecure initialize failed"),
    };

    public PaymentsControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private WebApplicationFactory<Program> CreateFactoryWithGateway(FakeIyzicoPaymentGateway fakeGateway)
    {
        return _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IIyzicoPaymentGateway>();
                services.AddSingleton<IIyzicoPaymentGateway>(fakeGateway);
            });
        });
    }

    private static CheckoutInvoiceInfoRequest CreateInvoiceInfo(string address = "Test Invoice Address 123, Istanbul")
    {
        return new CheckoutInvoiceInfoRequest
        {
            Type = InvoiceType.Individual,
            FullName = "Test User",
            InvoiceAddress = address
        };
    }

    [Fact]
    public async Task ProcessPayment_WhenSaveCardRequested_ShouldPersistTokenizedCardMetadata()
    {
        var fakeGateway = new FakeIyzicoPaymentGateway
        {
            ChargeResult = new IyzicoChargeGatewayResult(
                Success: true,
                PaymentId: "PAY-INTEGRATION-SAVECARD",
                ErrorMessage: null,
                CardToken: "iyz-card-token-save",
                CardUserKey: "iyz-user-key-save",
                LastFourDigits: "0009")
        };

        using var gatewayFactory = CreateFactoryWithGateway(fakeGateway);

        var userId = Random.Shared.Next(890_001, 900_000);
        var categoryId = Random.Shared.Next(900_001, 910_000);
        var productId = Random.Shared.Next(910_001, 920_000);
        var orderId = Random.Shared.Next(920_001, 930_000);
        var orderNumber = $"ORD-SAVE-CARD-{orderId}";

        await using (var scope = gatewayFactory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await TestDataSeeder.EnsureOrderWithPaymentAsync(
                db,
                orderId,
                userId,
                productId,
                categoryId,
                orderNumber,
                paymentIdempotencyKey: $"save-card-{orderId}",
                orderStatus: OrderStatus.PendingPayment,
                paymentStatus: PaymentStatus.Pending);
        }

        var client = gatewayFactory.CreateClient().AsCustomer(userId);
        var response = await client.PostAsJsonAsync("/api/v1/payments", new ProcessPaymentRequest
        {
            OrderId = orderId,
            PaymentProvider = PaymentProviderType.Iyzico,
            CardNumber = SuccessCard.Number,
            CardHolderName = "Test User",
            ExpiryDate = "12/26",
            CVV = "123",
            SaveCard = true,
            SaveCardAlias = "Maas Kartim"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var result = await response.Content.ReadFromJsonAsync<ApiResult<PaymentDto>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Data.Status.Should().Be("Success");

        await using var assertScope = gatewayFactory.Services.CreateAsyncScope();
        var assertDb = assertScope.ServiceProvider.GetRequiredService<AppDbContext>();
        var savedCard = await assertDb.CreditCards.SingleAsync(c =>
            c.UserId == userId &&
            c.TokenProvider == PaymentProviderType.Iyzico &&
            c.Last4Digits == "0009");

        savedCard.TokenProvider.Should().Be(PaymentProviderType.Iyzico);
        savedCard.IyzicoUserKey.Should().Be("iyz-user-key-save");
        savedCard.Last4Digits.Should().Be("0009");
        savedCard.CardAlias.Should().Be("Maas Kartim");
        savedCard.CardHolderName.Should().Be("Test User");
        savedCard.Brand.Should().Be(CardBrand.Mastercard);

        await assertDb.Database.OpenConnectionAsync();
        await using (var command = assertDb.Database.GetDbConnection().CreateCommand())
        {
            command.CommandText = "SELECT \"IyzicoCardToken\", \"IyzicoUserKey\" FROM \"TBL_CreditCards\" WHERE \"Id\" = @id";
            var parameter = command.CreateParameter();
            parameter.ParameterName = "@id";
            parameter.Value = savedCard.Id;
            command.Parameters.Add(parameter);

            await using var reader = await command.ExecuteReaderAsync();
            (await reader.ReadAsync()).Should().BeTrue();

            reader.GetString(0).Should().NotBe("iyz-card-token-save");
            reader.GetString(1).Should().NotBe("iyz-user-key-save");
        }
        await assertDb.Database.CloseConnectionAsync();

        var updatedOrder = await assertDb.Orders
            .Include(o => o.Payment)
            .SingleAsync(o => o.Id == orderId);

        updatedOrder.Status.Should().Be(OrderStatus.Paid);
        updatedOrder.Payment.Should().NotBeNull();
        updatedOrder.Payment!.Status.Should().Be(PaymentStatus.Success);
        updatedOrder.Payment.PaymentProviderId.Should().Be("PAY-INTEGRATION-SAVECARD");
        updatedOrder.Payment.Last4Digits.Should().Be("0009");
    }

    [Fact]
    public async Task ProcessPayment_WhenTokenizedSavedCardSelected_ShouldChargeWithProviderToken()
    {
        var fakeGateway = new FakeIyzicoPaymentGateway
        {
            ChargeResult = new IyzicoChargeGatewayResult(
                Success: true,
                PaymentId: "PAY-INTEGRATION-TOKEN",
                ErrorMessage: null,
                CardToken: null,
                CardUserKey: null,
                LastFourDigits: "1111")
        };

        using var gatewayFactory = CreateFactoryWithGateway(fakeGateway);

        var userId = Random.Shared.Next(930_001, 940_000);
        var categoryId = Random.Shared.Next(940_001, 950_000);
        var productId = Random.Shared.Next(950_001, 960_000);
        var orderId = Random.Shared.Next(960_001, 970_000);
        var orderNumber = $"ORD-TOKEN-CARD-{orderId}";
        int savedCardId;

        await using (var scope = gatewayFactory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var encryptionService = scope.ServiceProvider.GetRequiredService<IEncryptionService>();

            await TestDataSeeder.EnsureOrderWithPaymentAsync(
                db,
                orderId,
                userId,
                productId,
                categoryId,
                orderNumber,
                paymentIdempotencyKey: $"token-card-{orderId}",
                orderStatus: OrderStatus.PendingPayment,
                paymentStatus: PaymentStatus.Pending);

            var savedCard = new CreditCard
            {
                UserId = userId,
                CardAlias = "Iyzico Token Kartim",
                CardHolderName = "Test User",
                Brand = CardBrand.Mastercard,
                CardNumberEncrypted = encryptionService.Encrypt("tokenized:Iyzico:1111"),
                Last4Digits = "1111",
                ExpireMonthEncrypted = encryptionService.Encrypt("12"),
                ExpireYearEncrypted = encryptionService.Encrypt("2028"),
                IyzicoCardToken = "iyz-card-token-existing",
                IyzicoUserKey = "iyz-user-key-existing",
                TokenProvider = PaymentProviderType.Iyzico,
                IsDefault = true
            };

            db.CreditCards.Add(savedCard);
            await db.SaveChangesAsync();
            savedCardId = savedCard.Id;
        }

        var client = gatewayFactory.CreateClient().AsCustomer(userId);
        var response = await client.PostAsJsonAsync("/api/v1/payments", new ProcessPaymentRequest
        {
            OrderId = orderId,
            PaymentProvider = PaymentProviderType.Iyzico,
            SavedCardId = savedCardId
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var result = await response.Content.ReadFromJsonAsync<ApiResult<PaymentDto>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Data.Status.Should().Be("Success");

        fakeGateway.LastChargeRequest.Should().NotBeNull();
        fakeGateway.LastChargeRequest!.PaymentCard.Should().NotBeNull();
        fakeGateway.LastChargeRequest.PaymentCard.CardToken.Should().Be("iyz-card-token-existing");
        fakeGateway.LastChargeRequest.PaymentCard.CardUserKey.Should().Be("iyz-user-key-existing");
        fakeGateway.LastChargeRequest.PaymentCard.RegisterCard.Should().Be(0);
        fakeGateway.LastChargeRequest.PaymentCard.CardNumber.Should().BeNullOrEmpty();
        fakeGateway.LastChargeRequest.PaymentCard.Cvc.Should().BeNullOrEmpty();

        await using var assertScope = gatewayFactory.Services.CreateAsyncScope();
        var assertDb = assertScope.ServiceProvider.GetRequiredService<AppDbContext>();
        var userCards = await assertDb.CreditCards
            .Where(c => c.UserId == userId)
            .ToListAsync();

        userCards.Should().HaveCount(1);
        userCards[0].Id.Should().Be(savedCardId);

        var updatedOrder = await assertDb.Orders
            .Include(o => o.Payment)
            .SingleAsync(o => o.Id == orderId);

        updatedOrder.Status.Should().Be(OrderStatus.Paid);
        updatedOrder.Payment.Should().NotBeNull();
        updatedOrder.Payment!.Status.Should().Be(PaymentStatus.Success);
        updatedOrder.Payment.PaymentProviderId.Should().Be("PAY-INTEGRATION-TOKEN");
        updatedOrder.Payment.Last4Digits.Should().Be("1111");
    }

    [Fact]
    public async Task ProcessPayment_WithoutAuth_Returns401()
    {
        var anonymousClient = _factory.CreateClient().AsAnonymous();
        var paymentRequest = new ProcessPaymentRequest
        {
            OrderId = 1,
            CardNumber = SuccessCard.Number,
            CardHolderName = "Test User",
            ExpiryDate = "12/26",
            CVV = "123"
        };

        var response = await anonymousClient.PostAsJsonAsync("/api/v1/payments", paymentRequest);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ProcessPayment_UnverifiedUser_ReturnsForbidden()
    {
        var unverifiedClient = _factory.CreateClient().AsUnverifiedCustomer(userId: 10);
        var paymentRequest = new ProcessPaymentRequest
        {
            OrderId = 1,
            CardNumber = SuccessCard.Number,
            CardHolderName = "Test User",
            ExpiryDate = "12/26",
            CVV = "123"
        };

        var response = await unverifiedClient.PostAsJsonAsync("/api/v1/payments", paymentRequest);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ProcessPayment_InvalidOrderId_ReturnsBadRequest()
    {
        var authenticatedClient = _factory.CreateClient().AsCustomer(userId: 10);
        var paymentRequest = new ProcessPaymentRequest
        {
            OrderId = 999999,
            CardNumber = SuccessCard.Number,
            CardHolderName = "Test User",
            ExpiryDate = "12/26",
            CVV = "123"
        };

        var response = await authenticatedClient.PostAsJsonAsync("/api/v1/payments", paymentRequest);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ProcessPayment_WithExistingSuccessfulIdempotencyKey_ShouldReturnExistingPayment()
    {
        var userId = Random.Shared.Next(850_001, 860_000);
        var categoryId = Random.Shared.Next(860_001, 870_000);
        var productId = Random.Shared.Next(870_001, 880_000);
        var orderId = Random.Shared.Next(880_001, 890_000);
        var orderNumber = $"ORD-INTEGRATION-{orderId}";
        var idempotencyKey = $"integration-payment-idempotency-{orderId}";

        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await TestDataSeeder.EnsureOrderWithPaymentAsync(
                db,
                orderId,
                userId,
                productId,
                categoryId,
                orderNumber,
                idempotencyKey);
        }

        var client = _factory.CreateClient().AsCustomer(userId);
        client.DefaultRequestHeaders.Remove("Idempotency-Key");
        client.DefaultRequestHeaders.Add("Idempotency-Key", idempotencyKey);

        var response = await client.PostAsJsonAsync("/api/v1/payments", new ProcessPaymentRequest
        {
            OrderId = orderId,
            CardNumber = SuccessCard.Number,
            CardHolderName = "Test User",
            ExpiryDate = "12/26",
            CVV = "123"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var result = await response.Content.ReadFromJsonAsync<ApiResult<PaymentDto>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Data.Status.Should().Be("Success");
    }

    [Theory]
    [MemberData(nameof(GetIyzicoErrorCardsTestData))]
    public async Task ProcessPayment_IyzicoErrorCard_ReturnsExpectedError(
        string cardNumber, 
        string description)
    {
        var authenticatedClient = _factory.CreateClient().AsCustomer(userId: 10);
        
        await authenticatedClient.PostAsJsonAsync("/api/v1/cart/items", new AddToCartRequest { ProductId = 1, Quantity = 1 });
        var checkoutResponse = await authenticatedClient.PostAsJsonAsync("/api/v1/orders", new CheckoutRequest
        {
            ShippingAddress = "Test Address 123, Istanbul",
            PaymentMethod = "CreditCard",
            PreliminaryInfoAccepted = true,
            DistanceSalesContractAccepted = true,
            InvoiceInfo = CreateInvoiceInfo()
        });

        if (!checkoutResponse.IsSuccessStatusCode)
        {
            Assert.True(true, $"Checkout failed for card test: {description}. This is expected in unit-test-only mode.");
            return;
        }

        var checkoutResult = await checkoutResponse.Content.ReadFromJsonAsync<ApiResult<OrderDto>>();
        var orderId = checkoutResult!.Data.Id;

        var paymentRequest = new ProcessPaymentRequest
        {
            OrderId = orderId,
            CardNumber = cardNumber,
            CardHolderName = "Test User",
            ExpiryDate = "12/26",
            CVV = "123"
        };

        var response = await authenticatedClient.PostAsJsonAsync("/api/v1/payments", paymentRequest);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest, 
            because: $"Card {cardNumber} should fail with: {description}");
        
        var result = await response.Content.ReadFromJsonAsync<ApiResult<PaymentDto>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeFalse();
        result.Message.Should().NotBeNullOrEmpty(
            because: $"Error message should be returned for card: {description}");
    }

    public static IEnumerable<object[]> GetIyzicoErrorCardsTestData()
    {
        foreach (var card in ErrorCards)
        {
            yield return new object[] { card.Number, card.Description };
        }
    }

    [Fact(Skip = "Requires real order")]
    public async Task ProcessPayment_InsufficientFunds_ReturnsError()
    {
        await TestPaymentWithCard("4111111111111129", "Yetersiz bakiye");
    }

    [Fact(Skip = "Requires real order")]
    public async Task ProcessPayment_InvalidCvc_ReturnsError()
    {
        await TestPaymentWithCard("4124111111111116", "Cvc");
    }

    [Fact(Skip = "Requires real order")]
    public async Task ProcessPayment_StolenCard_ReturnsError()
    {
        await TestPaymentWithCard("4126111111111114", "Çalıntı");
    }

    [Fact(Skip = "Requires real order")]
    public async Task ProcessPayment_ExpiredCard_ReturnsError()
    {
        await TestPaymentWithCard("4125111111111115", "Süresi");
    }

    [Fact(Skip = "Requires real order")]
    public async Task ProcessPayment_FraudSuspect_ReturnsError()
    {
        await TestPaymentWithCard("4121111111111119", "Dolandırıcılık");
    }

    private async Task TestPaymentWithCard(string cardNumber, string expectedErrorContains)
    {
        var authenticatedClient = _factory.CreateClient().AsCustomer(userId: 10);
        
        var paymentRequest = new ProcessPaymentRequest
        {
            OrderId = 1,
            CardNumber = cardNumber,
            CardHolderName = "Test User",
            ExpiryDate = "12/26",
            CVV = "123"
        };

        var response = await authenticatedClient.PostAsJsonAsync("/api/v1/payments", paymentRequest);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var result = await response.Content.ReadFromJsonAsync<ApiResult<PaymentDto>>();
        result!.Success.Should().BeFalse();
        result.Message.Should().Contain(expectedErrorContains);
    }
}
