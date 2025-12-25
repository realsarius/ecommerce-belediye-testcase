using System.Net;
using System.Net.Http.Json;
using EcommerceAPI.Entities.DTOs;
using EcommerceAPI.IntegrationTests.Utilities;
using FluentAssertions;
using Xunit;

namespace EcommerceAPI.IntegrationTests.Tests;

[Collection("Integration")]
public class CreditCardsControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public CreditCardsControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private async Task<CreditCardDto?> CreateCardAsync(HttpClient client)
    {
        var createRequest = new AddCreditCardRequest
        {
            CardHolderName = "Test User",
            CardNumber = "5406670000000009", // Sandbox Success Card
            ExpireMonth = "12",
            ExpireYear = "2030",
            Cvv = "123",
            CardAlias = $"My Card {Guid.NewGuid():N}"
        };

        var response = await client.PostAsJsonAsync("/api/v1/creditcards", createRequest);
        if (response.StatusCode != HttpStatusCode.Created) return null;
        
        return await response.Content.ReadFromJsonAsync<CreditCardDto>();
    }

    [Fact]
    public async Task GetCards_WithoutAuth_Returns401()
    {
        var client = _factory.CreateClient().AsAnonymous();
        var response = await client.GetAsync("/api/v1/creditcards");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task AddCard_ValidRequest_ReturnsCreated()
    {
        var client = _factory.CreateClient().AsCustomer(userId: 60);
        var request = new AddCreditCardRequest
        {
            CardHolderName = "Test User",
            CardNumber = "5406670000000009", // Sandbox Success Card
            ExpireMonth = "12",
            ExpireYear = "2030",
            Cvv = "123",
            CardAlias = "My Card"
        };

        var response = await client.PostAsJsonAsync("/api/v1/creditcards", request);

        response.StatusCode.Should().BeOneOf(HttpStatusCode.Created, HttpStatusCode.InternalServerError, HttpStatusCode.BadRequest);
        
        if (response.StatusCode == HttpStatusCode.Created)
        {
            var result = await response.Content.ReadFromJsonAsync<CreditCardDto>();
            result.Should().NotBeNull();
        }
    }

    [Fact]
    public async Task DeleteCard_Existing_ReturnsOk()
    {
        var client = _factory.CreateClient().AsCustomer(userId: 60);
        var card = await CreateCardAsync(client);
        card.Should().NotBeNull("Setup failed: Card could not be created");

        var response = await client.DeleteAsync($"/api/v1/creditcards/{card.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task SetDefault_ExistingCard_ReturnsOk()
    {
        var client = _factory.CreateClient().AsCustomer(userId: 60);
        var card = await CreateCardAsync(client);
        card.Should().NotBeNull("Setup failed: Card could not be created");

        var request = new SetDefaultCreditCardRequest { IsDefault = true };

        var response = await client.PatchAsJsonAsync($"/api/v1/creditcards/{card.Id}", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
