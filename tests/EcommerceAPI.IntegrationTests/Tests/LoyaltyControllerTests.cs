using System.Net;
using System.Net.Http.Json;
using EcommerceAPI.DataAccess.Concrete.EntityFramework.Contexts;
using EcommerceAPI.Entities.Concrete;
using EcommerceAPI.Entities.DTOs;
using EcommerceAPI.Entities.Enums;
using EcommerceAPI.IntegrationTests.Utilities;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace EcommerceAPI.IntegrationTests.Tests;

[Collection("Integration")]
public class LoyaltyControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public LoyaltyControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetSummary_ShouldReturnBalanceAndRecentTransactions()
    {
        var userId = Random.Shared.Next(913_001, 914_000);
        await SeedLoyaltyTransactionsAsync(userId);

        var client = _factory.CreateClient().AsCustomer(userId);
        var response = await client.GetAsync("/api/v1/loyalty/summary");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResult<LoyaltySummaryDto>>();
        result.Should().NotBeNull();
        result!.Data.AvailablePoints.Should().Be(700);
        result.Data.AvailableDiscountAmount.Should().Be(7m);
        result.Data.RecentTransactions.Count.Should().BeGreaterThanOrEqualTo(2);
    }

    private async Task SeedLoyaltyTransactionsAsync(int userId)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await TestDataSeeder.EnsureUserAsync(db, userId);

        db.LoyaltyTransactions.AddRange(
            new LoyaltyTransaction
            {
                UserId = userId,
                Type = LoyaltyTransactionType.Earned,
                Points = 1000,
                BalanceAfter = 1000,
                Description = "İlk siparişten kazanım"
            },
            new LoyaltyTransaction
            {
                UserId = userId,
                Type = LoyaltyTransactionType.Redeemed,
                Points = -300,
                BalanceAfter = 700,
                Description = "Checkout puan kullanımı"
            });

        await db.SaveChangesAsync();
    }
}
