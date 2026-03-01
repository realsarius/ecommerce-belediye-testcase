using EcommerceAPI.Business.Concrete;
using EcommerceAPI.DataAccess.Abstract;
using EcommerceAPI.Entities.Concrete;
using EcommerceAPI.Entities.Enums;
using EcommerceAPI.Core.Interfaces;
using FluentAssertions;
using Moq;

namespace EcommerceAPI.UnitTests;

public class CartManagerTests
{
    [Fact]
    public async Task GetCartAsync_WhenProductHasActiveCampaign_ShouldUseCampaignPrice()
    {
        var cartCacheMock = new Mock<ICartCacheService>();
        var productDalMock = new Mock<IProductDal>();

        cartCacheMock
            .Setup(x => x.GetCartItemsAsync(7))
            .ReturnsAsync(new Dictionary<int, int> { [15] = 2 });

        productDalMock
            .Setup(x => x.GetByIdWithDetailsAsync(15))
            .ReturnsAsync(new Product
            {
                Id = 15,
                Name = "Kampanyali Urun",
                Description = "d",
                Price = 1500m,
                Currency = "TRY",
                SKU = "SKU-15",
                IsActive = true,
                CampaignProducts =
                [
                    new CampaignProduct
                    {
                        CampaignPrice = 999m,
                        OriginalPriceSnapshot = 1500m,
                        Campaign = new Campaign
                        {
                            Name = "Aksam Flash Sale",
                            IsEnabled = true,
                            Status = CampaignStatus.Active,
                            StartsAt = DateTime.UtcNow.AddMinutes(-30),
                            EndsAt = DateTime.UtcNow.AddHours(2)
                        }
                    }
                ],
                Inventory = new Inventory
                {
                    QuantityAvailable = 10
                }
            });

        var manager = new CartManager(cartCacheMock.Object, productDalMock.Object);

        var result = await manager.GetCartAsync(7);

        result.Success.Should().BeTrue();
        result.Data.TotalAmount.Should().Be(1998m);
        result.Data.Items.Should().ContainSingle();
        result.Data.Items[0].UnitPrice.Should().Be(999m);
    }
}
