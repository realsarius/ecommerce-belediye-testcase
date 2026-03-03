using EcommerceAPI.Business.Concrete;
using EcommerceAPI.Core.CrossCuttingConcerns.Logging;
using EcommerceAPI.Core.Interfaces;
using EcommerceAPI.DataAccess.Abstract;
using EcommerceAPI.Entities.Concrete;
using EcommerceAPI.Entities.DTOs;
using EcommerceAPI.Entities.IntegrationEvents;
using FluentAssertions;
using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;

namespace EcommerceAPI.UnitTests;

public class ProductManagerTests
{
    private readonly Mock<IProductDal> _productDalMock = new();
    private readonly Mock<IInventoryDal> _inventoryDalMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<IAuditService> _auditServiceMock = new();
    private readonly Mock<IPublishEndpoint> _publishEndpointMock = new();
    private readonly IConfiguration _configuration;

    public ProductManagerTests()
    {
        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Cache:ProductListTTLMinutes"] = "5",
            })
            .Build();
    }

    private ProductManager CreateManager()
    {
        return new ProductManager(
            _productDalMock.Object,
            _inventoryDalMock.Object,
            _unitOfWorkMock.Object,
            _configuration,
            _auditServiceMock.Object,
            Mock.Of<ILogger<ProductManager>>(),
            _publishEndpointMock.Object);
    }

    [Fact]
    public async Task CreateProductAsync_ShouldPersistImagesVariantsAndDraftState()
    {
        Product? addedProduct = null;

        _productDalMock
            .Setup(x => x.AddAsync(It.IsAny<Product>()))
            .Callback<Product>(product =>
            {
                product.Id = 301;
                addedProduct = product;
            })
            .ReturnsAsync((Product product) => product);

        _inventoryDalMock
            .Setup(x => x.AddAsync(It.IsAny<Inventory>()))
            .ReturnsAsync((Inventory inventory) => inventory);

        var request = new CreateProductRequest
        {
            Name = "Deri Ceket",
            Description = "Aciklama",
            Price = 2499,
            Currency = "try",
            SKU = "CEKET-1",
            CategoryId = 7,
            InitialStock = 12,
            IsActive = false,
            Images =
            [
                new ProductImageInputDto { ImageUrl = "https://cdn.test/ceket-1.jpg", IsPrimary = true },
                new ProductImageInputDto { ImageUrl = "https://cdn.test/ceket-2.jpg", IsPrimary = false },
            ],
            Variants =
            [
                new ProductVariantInputDto { Name = "Beden", Value = "L" },
                new ProductVariantInputDto { Name = "Renk", Value = "Siyah" },
            ]
        };

        var result = await CreateManager().CreateProductAsync(request, sellerId: 55);

        result.Success.Should().BeTrue();
        addedProduct.Should().NotBeNull();
        addedProduct!.SellerId.Should().Be(55);
        addedProduct.IsActive.Should().BeFalse();
        addedProduct.Currency.Should().Be("TRY");
        addedProduct.Images.Should().HaveCount(2);
        addedProduct.Images.Should().ContainSingle(image => image.IsPrimary);
        addedProduct.Variants.Should().ContainSingle(variant => variant.Name == "Beden" && variant.Value == "L");
        result.Data.Images.Should().HaveCount(2);
        result.Data.PrimaryImageUrl.Should().Be("https://cdn.test/ceket-1.jpg");

        _publishEndpointMock.Verify(
            x => x.Publish(It.Is<ProductIndexSyncEvent>(evt => evt.ProductId == 301 && evt.Operation == ProductIndexOperations.Upsert), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task UpdateProductAsync_ShouldReplaceImagesVariantsAndUpdateSku()
    {
        var product = new Product
        {
            Id = 88,
            Name = "Eski Urun",
            Description = "Eski",
            Price = 100,
            Currency = "TRY",
            SKU = "OLD-1",
            CategoryId = 2,
            SellerId = 45,
            IsActive = true,
            Inventory = new Inventory
            {
                ProductId = 88,
                QuantityAvailable = 4,
                QuantityReserved = 0,
            },
            Images =
            [
                new ProductImage { Id = 1, ProductId = 88, ImageUrl = "https://cdn.test/old.jpg", IsPrimary = true, SortOrder = 0 }
            ],
            Variants =
            [
                new ProductVariant { Id = 1, ProductId = 88, Name = "Renk", Value = "Mavi", SortOrder = 0 }
            ]
        };

        _productDalMock
            .Setup(x => x.GetByIdForUpdateAsync(88))
            .ReturnsAsync(product);

        _inventoryDalMock
            .Setup(x => x.GetByProductIdAsync(88))
            .ReturnsAsync(product.Inventory);

        _productDalMock
            .Setup(x => x.GetByIdWithDetailsAsync(88))
            .ReturnsAsync(new Product
            {
                Id = product.Id,
                Name = "Yeni Urun",
                Description = "Yeni aciklama",
                Price = 150,
                Currency = "USD",
                SKU = "NEW-1",
                CategoryId = 5,
                SellerId = 45,
                IsActive = false,
                Inventory = new Inventory { ProductId = 88, QuantityAvailable = 9, QuantityReserved = 0 },
                Images =
                [
                    new ProductImage { Id = 2, ProductId = 88, ImageUrl = "https://cdn.test/new.jpg", IsPrimary = true, SortOrder = 0 }
                ],
                Variants =
                [
                    new ProductVariant { Id = 2, ProductId = 88, Name = "Beden", Value = "XL", SortOrder = 0 }
                ]
            });

        var request = new UpdateProductRequest
        {
            Name = "Yeni Urun",
            Description = "Yeni aciklama",
            Price = 150,
            Currency = "usd",
            SKU = "NEW-1",
            CategoryId = 5,
            IsActive = false,
            StockQuantity = 9,
            Images =
            [
                new ProductImageInputDto { ImageUrl = "https://cdn.test/new.jpg", IsPrimary = true }
            ],
            Variants =
            [
                new ProductVariantInputDto { Name = "Beden", Value = "XL" }
            ]
        };

        var result = await CreateManager().UpdateProductAsync(88, request, sellerId: 45);

        result.Success.Should().BeTrue();
        product.SKU.Should().Be("NEW-1");
        product.Currency.Should().Be("USD");
        product.IsActive.Should().BeFalse();
        product.Inventory!.QuantityAvailable.Should().Be(9);
        product.Images.Should().ContainSingle(image => image.ImageUrl == "https://cdn.test/new.jpg" && image.IsPrimary);
        product.Variants.Should().ContainSingle(variant => variant.Name == "Beden" && variant.Value == "XL");

        _publishEndpointMock.Verify(
            x => x.Publish(It.Is<ProductIndexSyncEvent>(evt => evt.ProductId == 88 && evt.Operation == ProductIndexOperations.Delete), It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
