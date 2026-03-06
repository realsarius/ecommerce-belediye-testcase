using EcommerceAPI.Business.Abstract;
using EcommerceAPI.DataAccess.Concrete.EntityFramework.Contexts;
using EcommerceAPI.Entities.Concrete;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EcommerceAPI.IntegrationTests.Tests;

[Collection("Integration")]
public class PlatformProductBackfillTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public PlatformProductBackfillTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task BackfillMissingSellerIdsAsync_WhenNullSellerProductExists_ShouldAssignPlatformSeller()
    {
        using var scope = _factory.Services.CreateScope();
        var services = scope.ServiceProvider;
        var db = services.GetRequiredService<AppDbContext>();

        var categoryId = await db.Categories
            .AsNoTracking()
            .Select(category => category.Id)
            .FirstAsync();

        var product = new Product
        {
            Name = "Backfill Integration Product",
            Description = "Backfill test urunu",
            Price = 199.99m,
            Currency = "TRY",
            SKU = $"BF-{Guid.NewGuid():N}"[..20],
            CategoryId = categoryId,
            SellerId = null,
            IsActive = true
        };

        db.Products.Add(product);
        await db.SaveChangesAsync();

        db.Inventories.Add(new Inventory
        {
            ProductId = product.Id,
            QuantityAvailable = 9,
            QuantityReserved = 0
        });
        await db.SaveChangesAsync();

        var backfillService = services.GetRequiredService<IPlatformProductBackfillService>();
        var snapshotProductIds = await backfillService.GetProductIdsWithoutSellerSnapshotAsync();
        snapshotProductIds.Should().Contain(product.Id);

        var backfillResult = await backfillService.BackfillMissingSellerIdsAsync();

        backfillResult.Success.Should().BeTrue();

        var refreshedProduct = await db.Products
            .AsNoTracking()
            .FirstAsync(item => item.Id == product.Id);

        refreshedProduct.SellerId.Should().NotBeNull();

        var assignedSeller = await db.SellerProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(profile => profile.Id == refreshedProduct.SellerId);

        assignedSeller.Should().NotBeNull();
        assignedSeller!.IsVerified.Should().BeTrue();
    }
}
