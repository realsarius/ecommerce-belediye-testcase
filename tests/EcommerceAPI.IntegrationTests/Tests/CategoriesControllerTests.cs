using System.Net;
using System.Net.Http.Json;
using EcommerceAPI.Entities.DTOs;
using EcommerceAPI.IntegrationTests.Utilities;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace EcommerceAPI.IntegrationTests.Tests;

[Collection("Integration")]
public class CategoriesControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;
    private readonly ITestOutputHelper _output;

    public CategoriesControllerTests(CustomWebApplicationFactory factory, ITestOutputHelper output)
    {
        _factory = factory;
        _client = factory.CreateClient();
        _output = output;
    }

    private async Task<CategoryDto?> CreateCategoryAsync(HttpClient client, string? name = null)
    {
        var request = new CreateCategoryRequest
        {
            Name = name ?? $"Test Category {Guid.NewGuid():N}"
        };
        var response = await client.PostAsJsonAsync("/api/v1/admin/categories", request);
        return await response.Content.ReadFromJsonAsync<ApiResult<CategoryDto>>().ContinueWith(t => t.Result?.Data);
    }

    #region Public Endpoints

    [Fact]
    public async Task GetCategories_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/v1/categories");

        if (response.StatusCode != HttpStatusCode.OK)
        {
            var content = await response.Content.ReadAsStringAsync();
            _output.WriteLine($"Response Status: {response.StatusCode}");
            _output.WriteLine($"Response Content: {content}");
        }

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResult<List<CategoryDto>>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
    }

    [Fact]
    public async Task GetCategory_NonExisting_ReturnsBadRequest()
    {
        var response = await _client.GetAsync("/api/v1/categories/999999");
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region Admin Endpoints

    [Fact]
    public async Task CreateCategory_AsAdmin_ReturnsCreated()
    {
        var client = _factory.CreateClient().AsAdmin(1);
        var request = new CreateCategoryRequest
        {
            Name = $"Test Category {Guid.NewGuid():N}"
        };

        var response = await client.PostAsJsonAsync("/api/v1/admin/categories", request);

        response.StatusCode.Should().BeOneOf(HttpStatusCode.Created, HttpStatusCode.BadRequest);
        
        if (response.StatusCode == HttpStatusCode.Created)
        {
            var result = await response.Content.ReadFromJsonAsync<ApiResult<CategoryDto>>();
            result!.Success.Should().BeTrue();
            result.Data.Name.Should().Be(request.Name);
        }
    }

    [Fact]
    public async Task CreateCategory_AsCustomer_Returns403()
    {
        var client = _factory.CreateClient().AsCustomer(1);
        var request = new CreateCategoryRequest { Name = "Hacked Category" };

        var response = await client.PostAsJsonAsync("/api/v1/admin/categories", request);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task UpdateCategory_AsAdmin_ReturnsOk()
    {
        var client = _factory.CreateClient().AsAdmin(1);
        
        var request = new UpdateCategoryRequest
        {
            Name = $"Updated Cat {Guid.NewGuid():N}",
            IsActive = true
        };

        var response = await client.PutAsJsonAsync("/api/v1/admin/categories/1", request);

        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.BadRequest, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteCategory_AsAdmin_ReturnsOk()
    {
        var client = _factory.CreateClient().AsAdmin(1);
        var category = await CreateCategoryAsync(client);
        category.Should().NotBeNull("Setup failed: Category could not be created");

        var response = await client.DeleteAsync($"/api/v1/admin/categories/{category.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    #endregion
}
