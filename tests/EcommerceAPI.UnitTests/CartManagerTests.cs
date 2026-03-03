using EcommerceAPI.Business.Concrete;
using EcommerceAPI.DataAccess.Abstract;
using EcommerceAPI.Entities.Concrete;
using EcommerceAPI.Entities.DTOs;
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
        var orderDalMock = new Mock<IOrderDal>();

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

        var manager = new CartManager(cartCacheMock.Object, productDalMock.Object, orderDalMock.Object);

        var result = await manager.GetCartAsync(7);

        result.Success.Should().BeTrue();
        result.Data.TotalAmount.Should().Be(1998m);
        result.Data.Items.Should().ContainSingle();
        result.Data.Items[0].UnitPrice.Should().Be(999m);
    }

    [Fact]
    public async Task ReorderAsync_WhenOrderBelongsToAnotherUser_ShouldReturnNotFound()
    {
        var cartCacheMock = new Mock<ICartCacheService>();
        var productDalMock = new Mock<IProductDal>();
        var orderDalMock = new Mock<IOrderDal>();

        orderDalMock
            .Setup(x => x.GetByIdWithDetailsAsync(44))
            .ReturnsAsync(new Order
            {
                Id = 44,
                UserId = 99,
                OrderItems =
                [
                    new OrderItem
                    {
                        ProductId = 15,
                        Quantity = 1
                    }
                ]
            });

        var manager = new CartManager(cartCacheMock.Object, productDalMock.Object, orderDalMock.Object);

        var result = await manager.ReorderAsync(7, new ReorderCartRequest { OrderId = 44 });

        result.Success.Should().BeFalse();
        result.Message.Should().Be("Sipariş bulunamadı");
    }

    [Fact]
    public async Task ReorderAsync_WhenOrderContainsUnavailableAndPartialStockItems_ShouldAddWhatItCan()
    {
        var cartCacheMock = new Mock<ICartCacheService>();
        var productDalMock = new Mock<IProductDal>();
        var orderDalMock = new Mock<IOrderDal>();

        orderDalMock
            .Setup(x => x.GetByIdWithDetailsAsync(12))
            .ReturnsAsync(new Order
            {
                Id = 12,
                UserId = 7,
                OrderItems =
                [
                    new OrderItem
                    {
                        ProductId = 101,
                        Quantity = 2,
                        Product = new Product { Id = 101, Name = "Klavye" }
                    },
                    new OrderItem
                    {
                        ProductId = 202,
                        Quantity = 3,
                        Product = new Product { Id = 202, Name = "Mouse" }
                    },
                    new OrderItem
                    {
                        ProductId = 303,
                        Quantity = 1,
                        Product = new Product { Id = 303, Name = "Kulaklık" }
                    }
                ]
            });

        productDalMock
            .Setup(x => x.GetByIdsWithInventoryAsync(It.Is<List<int>>(ids =>
                ids.Count == 3 &&
                ids.Contains(101) &&
                ids.Contains(202) &&
                ids.Contains(303))))
            .ReturnsAsync(
            [
                new Product
                {
                    Id = 101,
                    Name = "Klavye",
                    IsActive = true,
                    Inventory = new Inventory { QuantityAvailable = 10 }
                },
                new Product
                {
                    Id = 202,
                    Name = "Mouse",
                    IsActive = true,
                    Inventory = new Inventory { QuantityAvailable = 4 }
                },
                new Product
                {
                    Id = 303,
                    Name = "Kulaklık",
                    IsActive = false,
                    Inventory = new Inventory { QuantityAvailable = 5 }
                }
            ]);

        cartCacheMock
            .Setup(x => x.GetCartItemsAsync(7))
            .ReturnsAsync(new Dictionary<int, int>
            {
                [202] = 3
            });

        var manager = new CartManager(cartCacheMock.Object, productDalMock.Object, orderDalMock.Object);

        var result = await manager.ReorderAsync(7, new ReorderCartRequest { OrderId = 12 });

        result.Success.Should().BeTrue();
        result.Data.RequestedCount.Should().Be(3);
        result.Data.AddedCount.Should().Be(2);
        result.Data.SkippedCount.Should().Be(2);
        result.Data.SkippedProducts.Should().ContainSingle(x => x.ProductId == 202 && x.Reason.Contains("yalnızca 1 adedi"));
        result.Data.SkippedProducts.Should().ContainSingle(x => x.ProductId == 303 && x.Reason.Contains("artık satışta değil"));

        cartCacheMock.Verify(x => x.IncrementItemQuantityAsync(7, 101, 2), Times.Once);
        cartCacheMock.Verify(x => x.IncrementItemQuantityAsync(7, 202, 1), Times.Once);
    }
}
