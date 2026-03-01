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
public class CampaignsControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public CampaignsControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task CreateCampaign_AsAdmin_ShouldCreateCampaign()
    {
        var adminId = Random.Shared.Next(970_001, 971_000);
        var categoryId = Random.Shared.Next(971_001, 972_000);
        var productId = Random.Shared.Next(972_001, 973_000);

        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await TestDataSeeder.EnsureUserAsync(db, adminId, "Admin");
            await TestDataSeeder.EnsureProductWithStockAsync(db, productId, categoryId, 20);
        }

        var client = _factory.CreateClient().AsAdmin(adminId);
        var response = await client.PostAsJsonAsync("/api/v1/admin/campaigns", new CreateCampaignRequest
        {
            Name = "Gece Fırsatı",
            Description = "Sınırlı süreli kampanya",
            BadgeText = "Geceye özel",
            Type = CampaignType.FlashSale,
            StartsAt = DateTime.UtcNow.AddMinutes(-30),
            EndsAt = DateTime.UtcNow.AddHours(8),
            Products =
            [
                new CreateCampaignProductRequest
                {
                    ProductId = productId,
                    CampaignPrice = 79.99m,
                    IsFeatured = true
                }
            ]
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = await response.Content.ReadFromJsonAsync<ApiResult<CampaignDto>>();
        result.Should().NotBeNull();
        result!.Data.Name.Should().Be("Gece Fırsatı");
        result.Data.Status.Should().Be(CampaignStatus.Active);
    }

    [Fact]
    public async Task GetActiveCampaigns_ShouldReturnOnlyActiveCampaigns()
    {
        var adminId = Random.Shared.Next(973_001, 974_000);
        var categoryId = Random.Shared.Next(974_001, 975_000);
        var productId = Random.Shared.Next(975_001, 976_000);

        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await TestDataSeeder.EnsureUserAsync(db, adminId, "Admin");
            await TestDataSeeder.EnsureProductWithStockAsync(db, productId, categoryId, 15);
        }

        var adminClient = _factory.CreateClient().AsAdmin(adminId);
        await adminClient.PostAsJsonAsync("/api/v1/admin/campaigns", new CreateCampaignRequest
        {
            Name = "Aktif Kampanya",
            StartsAt = DateTime.UtcNow.AddMinutes(-10),
            EndsAt = DateTime.UtcNow.AddHours(2),
            Products =
            [
                new CreateCampaignProductRequest
                {
                    ProductId = productId,
                    CampaignPrice = 89.99m
                }
            ]
        });

        var response = await _factory.CreateClient().GetAsync("/api/v1/campaigns/active");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ApiResult<List<CampaignDto>>>();
        result.Should().NotBeNull();
        result!.Data.Should().Contain(x => x.Name == "Aktif Kampanya");
    }

    [Fact]
    public async Task TrackCampaignInteraction_WhenCampaignExists_ReturnsOk()
    {
        var adminId = Random.Shared.Next(976_001, 977_000);
        var categoryId = Random.Shared.Next(977_001, 978_000);
        var productId = Random.Shared.Next(978_001, 979_000);

        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await TestDataSeeder.EnsureUserAsync(db, adminId, "Admin");
            await TestDataSeeder.EnsureProductWithStockAsync(db, productId, categoryId, 15);
        }

        var adminClient = _factory.CreateClient().AsAdmin(adminId);
        var createResponse = await adminClient.PostAsJsonAsync("/api/v1/admin/campaigns", new CreateCampaignRequest
        {
            Name = "Tracking Kampanyası",
            StartsAt = DateTime.UtcNow.AddMinutes(-5),
            EndsAt = DateTime.UtcNow.AddHours(1),
            Products =
            [
                new CreateCampaignProductRequest
                {
                    ProductId = productId,
                    CampaignPrice = 89.99m,
                    IsFeatured = true
                }
            ]
        });

        var created = await createResponse.Content.ReadFromJsonAsync<ApiResult<CampaignDto>>();
        created.Should().NotBeNull();

        var response = await _factory.CreateClient().PostAsJsonAsync(
            $"/api/v1/campaigns/{created!.Data.Id}/interactions",
            new TrackCampaignInteractionRequest
            {
                InteractionType = "click",
                ProductId = productId
            });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
