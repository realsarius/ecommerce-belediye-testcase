using System.Net;
using System.Net.Http.Json;
using EcommerceAPI.DataAccess.Concrete.EntityFramework.Contexts;
using EcommerceAPI.Entities.DTOs;
using EcommerceAPI.IntegrationTests.Utilities;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EcommerceAPI.IntegrationTests.Tests;

[Collection("Integration")]
public class SellerAnalyticsControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public SellerAnalyticsControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetSummary_AsSellerWithProfile_ReturnsSummary()
    {
        const int userId = 3401;

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var sellerProfile = await TestDataSeeder.EnsureSellerProfileAsync(db, userId, $"Analytics Brand {Guid.NewGuid():N}");
            await TestDataSeeder.EnsureCategoryAsync(db, 93101, $"Analytics Category {Guid.NewGuid():N}");
            await TestDataSeeder.EnsureProductWithStockAsync(db, 93101, 93101, 12, sellerProfile.Id);
        }

        var client = _factory.CreateClient().AsSeller(userId);

        var response = await client.GetAsync("/api/v1/seller/analytics/summary");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ApiResult<SellerAnalyticsSummaryDto>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Data.TotalProducts.Should().BeGreaterThanOrEqualTo(1);
    }

    [Fact]
    public async Task GetTrends_AsSellerWithProfile_ReturnsRequestedTrendPoints()
    {
        const int userId = 3402;

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var sellerProfile = await TestDataSeeder.EnsureSellerProfileAsync(db, userId, $"Trend Brand {Guid.NewGuid():N}");
            var categoryId = 93102;
            await TestDataSeeder.EnsureCategoryAsync(db, categoryId, $"Trend Category {Guid.NewGuid():N}");
            await TestDataSeeder.EnsureProductWithStockAsync(db, 93102, categoryId, 8, sellerProfile.Id);
        }

        var client = _factory.CreateClient().AsSeller(userId);

        var response = await client.GetAsync("/api/v1/seller/analytics/trends?days=14");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ApiResult<List<SellerAnalyticsTrendPointDto>>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Data.Should().HaveCount(14);
        result.Data.Should().OnlyContain(point => point.Date != default);
    }

    [Fact]
    public async Task GetSummary_AsCustomer_ReturnsForbidden()
    {
        var client = _factory.CreateClient().AsCustomer(userId: 3403);

        var response = await client.GetAsync("/api/v1/seller/analytics/summary");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
