using System.Net;
using System.Net.Http.Json;
using EcommerceAPI.DataAccess.Concrete.EntityFramework.Contexts;
using EcommerceAPI.Entities.DTOs;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using EcommerceAPI.IntegrationTests.Utilities;

namespace EcommerceAPI.IntegrationTests.Tests;

[Collection("Integration")]
public class ShippingAddressControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;

    public ShippingAddressControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private async Task<int> EnsureUserExistsAsync(int userId)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await TestDataSeeder.EnsureUserAsync(db, userId);
        return userId;
    }

    [Fact]
    public async Task CreateAddress_ValidRequest_ReturnsOk()
    {
        int userId = 101;
        await EnsureUserExistsAsync(userId);
        
        var authenticatedClient = _factory.CreateClient().AsCustomer(userId);
        var createRequest = new CreateShippingAddressRequest
        {
            Title = "Home",
            FullName = "Test User",
            City = "Istanbul",
            District = "Kadikoy",
            AddressLine = "Test Street No:1",
            Phone = "5551112233",
            PostalCode = "34700",
            IsDefault = true
        };

        var response = await authenticatedClient.PostAsJsonAsync("/api/v1/ShippingAddress", createRequest);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var createdAddress = await response.Content.ReadFromJsonAsync<ShippingAddressDto>();
        createdAddress.Should().NotBeNull();
        createdAddress!.Id.Should().BeGreaterThan(0);
        createdAddress.City.Should().Be(createRequest.City);
    }

    [Fact]
    public async Task GetMyAddresses_ExistingUser_ReturnsOk()
    {
        int userId = 102;
        await EnsureUserExistsAsync(userId);
        var authenticatedClient = _factory.CreateClient().AsCustomer(userId);

        var addressRequest = new CreateShippingAddressRequest
        {
            Title = "Office",
            FullName = "Test User",
            City = "Ankara",
            District = "Cankaya",
            AddressLine = "Office Block A",
            Phone = "5559998877",
            IsDefault = true
        };
        await authenticatedClient.PostAsJsonAsync("/api/v1/ShippingAddress", addressRequest);

        var response = await authenticatedClient.GetAsync("/api/v1/ShippingAddress");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var addressList = await response.Content.ReadFromJsonAsync<List<ShippingAddressDto>>();
        addressList.Should().NotBeNull();
        addressList.Should().Contain(a => a.City == "Ankara");
    }

    [Fact]
    public async Task DeleteAddress_ExistingAddress_ReturnsOk()
    {
        int userId = 103;
        await EnsureUserExistsAsync(userId);
        var authenticatedClient = _factory.CreateClient().AsCustomer(userId);

        var addressRequest = new CreateShippingAddressRequest
        {
            Title = "Home",
            FullName = "Test User",
            City = "Izmir",
            District = "Karsiyaka",
            AddressLine = "Seaside Blvd",
            Phone = "5553334455",
            IsDefault = true
        };
        var createResponse = await authenticatedClient.PostAsJsonAsync("/api/v1/ShippingAddress", addressRequest);
        var createdAddress = await createResponse.Content.ReadFromJsonAsync<ShippingAddressDto>();
        createdAddress.Should().NotBeNull();

        var deleteResponse = await authenticatedClient.DeleteAsync($"/api/v1/ShippingAddress/{createdAddress!.Id}");

        deleteResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
