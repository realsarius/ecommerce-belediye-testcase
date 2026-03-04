using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using EcommerceAPI.Business.Abstract;
using EcommerceAPI.DataAccess.Concrete.EntityFramework.Contexts;
using EcommerceAPI.Entities.DTOs;
using EcommerceAPI.Entities.Enums;
using EcommerceAPI.IntegrationTests.Utilities;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EcommerceAPI.IntegrationTests.Tests;

[Collection("Integration")]
public class CreditCardsControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private const int TestUserId = 60;

    public CreditCardsControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private async Task EnsureTestUserExistsAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await TestDataSeeder.EnsureUserAsync(db, TestUserId);
    }

    private async Task<CreditCardDto> CreateTokenizedCardAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var creditCardService = scope.ServiceProvider.GetRequiredService<ICreditCardService>();

        var result = await creditCardService.SaveTokenizedCardAsync(TestUserId, new SaveTokenizedCreditCardRequest
        {
            CardAlias = $"My Card {Guid.NewGuid():N}",
            CardHolderName = "Test User",
            Brand = CardBrand.Mastercard,
            Last4Digits = "0009",
            ExpireMonth = "12",
            ExpireYear = "2030",
            TokenProvider = PaymentProviderType.Iyzico,
            IyzicoCardToken = $"token-{Guid.NewGuid():N}",
            IyzicoUserKey = $"user-{Guid.NewGuid():N}",
            IsDefault = false
        });

        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        return result.Data!;
    }

    [Fact]
    public async Task GetCards_WithoutAuth_Returns401()
    {
        var client = _factory.CreateClient().AsAnonymous();
        var response = await client.GetAsync("/api/v1/creditcards");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task AddCard_ManualEntry_ReturnsBadRequest()
    {
        await EnsureTestUserExistsAsync();
        var client = _factory.CreateClient().AsCustomer(userId: TestUserId);
        
        var request = new AddCreditCardRequest
        {
            CardHolderName = "Test User",
            CardNumber = "5406670000000009", // Sandbox Success Card
            ExpireMonth = "12",
            ExpireYear = "2030",
            CardAlias = "My Card"
        };

        var response = await client.PostAsJsonAsync("/api/v1/creditcards", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var responseBody = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(responseBody);
        document.RootElement.GetProperty("message").GetString()
            .Should().Contain("checkout");
    }

    [Fact]
    public async Task DeleteCard_Existing_ReturnsOk()
    {
        await EnsureTestUserExistsAsync();
        var client = _factory.CreateClient().AsCustomer(userId: TestUserId);
        
        var card = await CreateTokenizedCardAsync();

        var response = await client.DeleteAsync($"/api/v1/creditcards/{card.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task SetDefault_ExistingCard_ReturnsOk()
    {
        await EnsureTestUserExistsAsync();
        var client = _factory.CreateClient().AsCustomer(userId: TestUserId);
        
        var card = await CreateTokenizedCardAsync();

        var request = new SetDefaultCreditCardRequest { IsDefault = true };

        var response = await client.PatchAsJsonAsync($"/api/v1/creditcards/{card.Id}", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
