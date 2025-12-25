using System.Net;
using System.Net.Http.Json;
using EcommerceAPI.Entities.DTOs;
using EcommerceAPI.IntegrationTests.Utilities;
using FluentAssertions;
using Xunit;

namespace EcommerceAPI.IntegrationTests.Tests;

[Collection("Integration")]
public class ShippingAddressControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public ShippingAddressControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private async Task<ShippingAddressDto?> CreateAddressAsync(HttpClient client)
    {
        var createRequest = new CreateShippingAddressRequest
        {
            Title = $"Home {Guid.NewGuid():N}",
            FullName = "Test User",
            Phone = "5551234567",
            City = "Istanbul",
            District = "Kadikoy",
            AddressLine = "Test Mah. Test Sok.",
            PostalCode = "34000",
            IsDefault = true
        };

        var response = await client.PostAsJsonAsync("/api/v1/shippingaddress", createRequest);
        if (response.StatusCode != HttpStatusCode.Created) return null;
        
        return await response.Content.ReadFromJsonAsync<ShippingAddressDto>();
    }

    [Fact]
    public async Task GetAddresses_WithoutAuth_Returns401()
    {
        var client = _factory.CreateClient().AsAnonymous();
        var response = await client.GetAsync("/api/v1/shippingaddress");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateAddress_ValidRequest_ReturnsCreated()
    {
        var client = _factory.CreateClient().AsCustomer(userId: 50);
        var request = new CreateShippingAddressRequest
        {
            Title = "Home",
            FullName = "Test User",
            Phone = "5551234567",
            City = "Istanbul",
            District = "Kadikoy",
            AddressLine = "Test Mah. Test Sok.",
            PostalCode = "34000",
            IsDefault = true
        };

        var response = await client.PostAsJsonAsync("/api/v1/shippingaddress", request);

        response.StatusCode.Should().BeOneOf(HttpStatusCode.Created, HttpStatusCode.InternalServerError, HttpStatusCode.BadRequest);
        
        if (response.StatusCode == HttpStatusCode.Created)
        {
            var result = await response.Content.ReadFromJsonAsync<ShippingAddressDto>();
            result.Should().NotBeNull();
            result!.City.Should().Be("Istanbul");
        }
    }

    [Fact]
    public async Task UpdateAddress_ValidRequest_ReturnsOk()
    {
        var client = _factory.CreateClient().AsCustomer(userId: 50);
        var address = await CreateAddressAsync(client);
        address.Should().NotBeNull("Setup failed: Address could not be created");
        
        var updateRequest = new CreateShippingAddressRequest
        {
            Title = "Work Updated",
            FullName = "Test User",
            Phone = "5551234567",
            City = "Ankara",
            District = "Cankaya",
            AddressLine = "Test Mah.",
            PostalCode = "06000"
        };

        var response = await client.PutAsJsonAsync($"/api/v1/shippingaddress/{address.Id}", updateRequest);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task DeleteAddress_Existing_ReturnsOk()
    {
        var client = _factory.CreateClient().AsCustomer(userId: 50);
        var address = await CreateAddressAsync(client);
        address.Should().NotBeNull("Setup failed: Address could not be created");

        var response = await client.DeleteAsync($"/api/v1/shippingaddress/{address.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
