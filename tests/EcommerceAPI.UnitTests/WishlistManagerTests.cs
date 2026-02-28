using EcommerceAPI.Business.Concrete;
using EcommerceAPI.Business.Mappers;
using EcommerceAPI.Core.CrossCuttingConcerns.Logging;
using EcommerceAPI.Core.Interfaces;
using EcommerceAPI.DataAccess.Abstract;
using EcommerceAPI.Entities.Concrete;
using EcommerceAPI.Entities.DTOs;
using EcommerceAPI.Entities.IntegrationEvents;
using FluentAssertions;
using MassTransit;
using Microsoft.Extensions.Logging;
using Moq;

namespace EcommerceAPI.UnitTests;

public class WishlistManagerTests
{
    private readonly Mock<IWishlistDal> _wishlistDalMock;
    private readonly Mock<IWishlistCollectionDal> _wishlistCollectionDalMock;
    private readonly Mock<IWishlistItemDal> _wishlistItemDalMock;
    private readonly Mock<IProductDal> _productDalMock;
    private readonly Mock<IWishlistMapper> _mapperMock;
    private readonly Mock<IUnitOfWork> _uowMock;
    private readonly Mock<IAuditService> _auditServiceMock;
    private readonly Mock<ILogger<WishlistManager>> _loggerMock;
    private readonly Mock<IPublishEndpoint> _publishEndpointMock;
    private readonly Mock<ICartCacheService> _cartCacheServiceMock;
    private readonly WishlistManager _manager;

    public WishlistManagerTests()
    {
        _wishlistDalMock = new Mock<IWishlistDal>();
        _wishlistCollectionDalMock = new Mock<IWishlistCollectionDal>();
        _wishlistItemDalMock = new Mock<IWishlistItemDal>();
        _productDalMock = new Mock<IProductDal>();
        _mapperMock = new Mock<IWishlistMapper>();
        _uowMock = new Mock<IUnitOfWork>();
        _auditServiceMock = new Mock<IAuditService>();
        _loggerMock = new Mock<ILogger<WishlistManager>>();
        _publishEndpointMock = new Mock<IPublishEndpoint>();
        _cartCacheServiceMock = new Mock<ICartCacheService>();
        _publishEndpointMock
            .Setup(x => x.Publish(It.IsAny<WishlistItemAddedEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _publishEndpointMock
            .Setup(x => x.Publish(It.IsAny<WishlistItemRemovedEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _wishlistCollectionDalMock
            .Setup(x => x.GetOrCreateDefaultCollectionAsync(It.IsAny<int>()))
            .ReturnsAsync((int wishlistId) => new WishlistCollection
            {
                Id = wishlistId + 1000,
                WishlistId = wishlistId,
                Name = "Favorilerim",
                IsDefault = true
            });

        _manager = new WishlistManager(
            _wishlistDalMock.Object,
            _wishlistCollectionDalMock.Object,
            _wishlistItemDalMock.Object,
            _productDalMock.Object,
            _mapperMock.Object,
            _uowMock.Object,
            _auditServiceMock.Object,
            _loggerMock.Object,
            _publishEndpointMock.Object,
            _cartCacheServiceMock.Object
        );
    }

    [Fact]
    public async Task GetWishlistByUserIdAsync_WhenNoWishlistExists_ReturnsEmptyWishlist()
    {
        const int userId = 1;

        _wishlistDalMock.Setup(d => d.GetAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Wishlist, bool>>>()))
            .ReturnsAsync((Wishlist?)null);

        var result = await _manager.GetWishlistByUserIdAsync(userId);

        result.Success.Should().BeTrue();
        result.Data.UserId.Should().Be(userId);
        result.Data.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task EnableSharingAsync_WhenWishlistExists_ReturnsShareToken()
    {
        const int userId = 1;
        var wishlist = new Wishlist { Id = 10, UserId = userId, IsPublic = false, ShareToken = null };

        _wishlistDalMock
            .Setup(d => d.GetOrCreateByUserIdAsync(userId))
            .ReturnsAsync(wishlist);

        var result = await _manager.EnableSharingAsync(userId);

        result.Success.Should().BeTrue();
        result.Data.IsPublic.Should().BeTrue();
        result.Data.ShareToken.Should().NotBeNull();
        result.Data.SharePath.Should().Contain("/wishlist/share/");
        _wishlistDalMock.Verify(d => d.Update(It.Is<Wishlist>(w =>
            w.Id == wishlist.Id &&
            w.IsPublic &&
            w.ShareToken.HasValue)), Times.Once);
        _uowMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task GetPublicWishlistByShareTokenAsync_WhenWishlistIsPublic_ReturnsSharedWishlist()
    {
        var shareToken = Guid.NewGuid();
        var wishlist = new Wishlist
        {
            Id = 10,
            UserId = 1,
            IsPublic = true,
            ShareToken = shareToken,
            User = new User
            {
                Id = 1,
                FirstName = "Ada",
                LastName = "Lovelace",
                Email = "ada@example.com"
            }
        };
        var addedAt = DateTime.UtcNow;
        var pagedItems = new List<WishlistItem>
        {
            new()
            {
                Id = 3,
                ProductId = 3,
                WishlistId = wishlist.Id,
                AddedAt = addedAt,
                AddedAtPrice = 300m,
                Product = new Product { Id = 3, Name = "P3", Price = 300m, Currency = "TRY", IsActive = true }
            },
            new()
            {
                Id = 2,
                ProductId = 2,
                WishlistId = wishlist.Id,
                AddedAt = addedAt.AddMinutes(-1),
                AddedAtPrice = 200m,
                Product = new Product { Id = 2, Name = "P2", Price = 200m, Currency = "TRY", IsActive = true }
            }
        };

        _wishlistDalMock
            .Setup(d => d.GetByShareTokenAsync(shareToken))
            .ReturnsAsync(wishlist);

        _wishlistItemDalMock
            .Setup(d => d.GetPagedByWishlistIdAsync(wishlist.Id, null, null, 2, null))
            .ReturnsAsync(pagedItems);

        _mapperMock
            .Setup(m => m.ToWishlistItemDto(It.IsAny<WishlistItem>()))
            .Returns((WishlistItem item) => new WishlistItemDto
            {
                Id = item.Id,
                ProductId = item.ProductId,
                ProductName = item.Product?.Name ?? string.Empty,
                ProductPrice = item.Product?.Price ?? 0,
                ProductCurrency = item.Product?.Currency ?? "TRY",
                IsAvailable = item.Product?.IsActive == true,
                AddedAt = item.AddedAt,
                AddedAtPrice = item.AddedAtPrice
            });

        var result = await _manager.GetPublicWishlistByShareTokenAsync(shareToken, null, 1);

        result.Success.Should().BeTrue();
        result.Data.OwnerDisplayName.Should().Be("Ada Lovelace");
        result.Data.Items.Should().ContainSingle();
        result.Data.HasMore.Should().BeTrue();
        result.Data.NextCursor.Should().NotBeNull();
    }

    [Fact]
    public async Task AddItemToWishlistAsync_WhenProductDoesNotExist_ReturnsError()
    {
        const int userId = 1;
        const int productId = 99;

        _productDalMock.Setup(d => d.GetAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Product, bool>>>()))
            .ReturnsAsync((Product?)null);

        var result = await _manager.AddItemToWishlistAsync(userId, productId);

        result.Success.Should().BeFalse();
        result.Message.Should().Be(EcommerceAPI.Business.Constants.Messages.ProductNotFound);
    }

    [Fact]
    public async Task AddItemToWishlistAsync_WhenProductValidAndNew_AddsItemAndSyncsCount()
    {
        const int userId = 1;
        const int productId = 2;
        const decimal productPrice = 299.99m;
        var wishlist = new Wishlist { Id = 10, UserId = userId };
        var product = new Product { Id = productId, Price = productPrice, IsActive = true };
        var defaultCollection = new WishlistCollection { Id = 1010, WishlistId = wishlist.Id, Name = "Favorilerim", IsDefault = true };

        _productDalMock.SetupSequence(d => d.GetAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Product, bool>>>()))
            .ReturnsAsync(product)
            .ReturnsAsync(product);

        _wishlistDalMock.Setup(d => d.GetOrCreateByUserIdAsync(userId))
            .ReturnsAsync(wishlist);
        _wishlistCollectionDalMock.Setup(d => d.GetOrCreateDefaultCollectionAsync(wishlist.Id))
            .ReturnsAsync(defaultCollection);

        _wishlistItemDalMock.SetupSequence(d => d.CountAsync(It.IsAny<System.Linq.Expressions.Expression<Func<WishlistItem, bool>>>()))
            .ReturnsAsync(0)
            .ReturnsAsync(1);

        _wishlistItemDalMock.Setup(d => d.AddIfNotExistsAsync(It.IsAny<WishlistItem>()))
            .ReturnsAsync(true);

        var result = await _manager.AddItemToWishlistAsync(userId, productId);

        result.Success.Should().BeTrue();
        _wishlistItemDalMock.Verify(d => d.AddIfNotExistsAsync(It.Is<WishlistItem>(i =>
            i.ProductId == productId &&
            i.WishlistId == wishlist.Id &&
            i.CollectionId == defaultCollection.Id &&
            i.AddedAtPrice == productPrice)), Times.Once);
        _productDalMock.Verify(d => d.Update(It.Is<Product>(p =>
            p.Id == productId &&
            p.WishlistCount == 1)), Times.Once);
        _uowMock.Verify(u => u.SaveChangesAsync(), Times.Once);
        _uowMock.Verify(u => u.BeginTransactionAsync(), Times.Once);
        _uowMock.Verify(u => u.CommitTransactionAsync(), Times.Once);
        _publishEndpointMock.Verify(x => x.Publish(It.Is<WishlistItemAddedEvent>(e =>
            e.UserId == userId &&
            e.WishlistId == wishlist.Id &&
            e.ProductId == productId &&
            e.PriceAtTime == productPrice), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AddItemToWishlistAsync_WhenProductAlreadyExists_ReturnsIdempotentSuccess()
    {
        const int userId = 1;
        const int productId = 2;
        var wishlist = new Wishlist { Id = 10, UserId = userId };
        var defaultCollection = new WishlistCollection { Id = 1010, WishlistId = wishlist.Id, Name = "Favorilerim", IsDefault = true };

        _productDalMock.Setup(d => d.GetAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Product, bool>>>()))
            .ReturnsAsync(new Product { Id = productId, Price = 100m, IsActive = true });

        _wishlistDalMock.Setup(d => d.GetOrCreateByUserIdAsync(userId))
            .ReturnsAsync(wishlist);
        _wishlistCollectionDalMock.Setup(d => d.GetOrCreateDefaultCollectionAsync(wishlist.Id))
            .ReturnsAsync(defaultCollection);

        _wishlistItemDalMock.Setup(d => d.CountAsync(It.IsAny<System.Linq.Expressions.Expression<Func<WishlistItem, bool>>>()))
            .ReturnsAsync(1);

        _wishlistItemDalMock.Setup(d => d.AddIfNotExistsAsync(It.IsAny<WishlistItem>()))
            .ReturnsAsync(false);

        var result = await _manager.AddItemToWishlistAsync(userId, productId);

        result.Success.Should().BeTrue();
        _productDalMock.Verify(d => d.Update(It.IsAny<Product>()), Times.Never);
        _uowMock.Verify(u => u.SaveChangesAsync(), Times.Never);
        _publishEndpointMock.Verify(x => x.Publish(It.IsAny<WishlistItemAddedEvent>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AddItemToWishlistAsync_WhenProductInactive_ReturnsError()
    {
        const int userId = 1;
        const int productId = 2;

        _productDalMock.Setup(d => d.GetAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Product, bool>>>()))
            .ReturnsAsync(new Product { Id = productId, IsActive = false });

        var result = await _manager.AddItemToWishlistAsync(userId, productId);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("satışta değil");
    }

    [Fact]
    public async Task AddItemToWishlistAsync_WhenWishlistLimitReached_ReturnsError()
    {
        const int userId = 1;
        const int productId = 2;
        var wishlist = new Wishlist { Id = 10, UserId = userId };
        var defaultCollection = new WishlistCollection { Id = 1010, WishlistId = wishlist.Id, Name = "Favorilerim", IsDefault = true };

        _productDalMock.Setup(d => d.GetAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Product, bool>>>()))
            .ReturnsAsync(new Product { Id = productId, Price = 100m, IsActive = true });

        _wishlistDalMock.Setup(d => d.GetOrCreateByUserIdAsync(userId))
            .ReturnsAsync(wishlist);
        _wishlistCollectionDalMock.Setup(d => d.GetOrCreateDefaultCollectionAsync(wishlist.Id))
            .ReturnsAsync(defaultCollection);

        _wishlistItemDalMock.Setup(d => d.CountAsync(It.IsAny<System.Linq.Expressions.Expression<Func<WishlistItem, bool>>>()))
            .ReturnsAsync(500);

        var result = await _manager.AddItemToWishlistAsync(userId, productId);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("maksimum kapasiteye");
    }

    [Fact]
    public async Task GetWishlistByUserIdAsync_KeepsInactiveProductsInResponse()
    {
        const int userId = 1;
        var wishlist = new Wishlist { Id = 10, UserId = userId };
        var activeProduct = new Product { Id = 1, Name = "Active", IsActive = true, Price = 100m };
        var inactiveProduct = new Product { Id = 2, Name = "Inactive", IsActive = false, Price = 200m };

        var items = new List<WishlistItem>
        {
            new() { Id = 1, ProductId = 1, WishlistId = 10, AddedAtPrice = 100m, AddedAt = DateTime.UtcNow },
            new() { Id = 2, ProductId = 2, WishlistId = 10, AddedAtPrice = 200m, AddedAt = DateTime.UtcNow }
        };

        _wishlistDalMock.Setup(d => d.GetAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Wishlist, bool>>>()))
            .ReturnsAsync(wishlist);

        _wishlistItemDalMock.Setup(d => d.GetByWishlistIdWithDetailsAsync(wishlist.Id, null))
            .ReturnsAsync(items);

        _productDalMock.Setup(d => d.GetAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Product, bool>>>()))
            .ReturnsAsync((System.Linq.Expressions.Expression<Func<Product, bool>> predicate) =>
            {
                var compiled = predicate.Compile();
                return new[] { activeProduct, inactiveProduct }.FirstOrDefault(compiled);
            });

        _mapperMock.Setup(m => m.ToWishlistDto(It.IsAny<Wishlist>()))
            .Returns((Wishlist w) => new WishlistDto
            {
                Id = w.Id,
                UserId = w.UserId,
                Items = w.Items.Select(i => new WishlistItemDto
                {
                    Id = i.Id,
                    ProductId = i.ProductId,
                    AddedAtPrice = i.AddedAtPrice
                }).ToList()
            });

        var result = await _manager.GetWishlistByUserIdAsync(userId);

        result.Success.Should().BeTrue();
        result.Data.Items.Should().HaveCount(2);
        result.Data.Items.Select(i => i.ProductId).Should().Contain(new[] { 1, 2 });
    }

    [Fact]
    public async Task GetWishlistByUserIdAsync_WhenPaginationRequested_ReturnsCursorMetadata()
    {
        const int userId = 1;
        var wishlist = new Wishlist { Id = 10, UserId = userId };
        var addedAt = DateTime.UtcNow;

        var items = new List<WishlistItem>
        {
            new() { Id = 3, ProductId = 3, WishlistId = 10, AddedAt = addedAt, AddedAtPrice = 300m, Product = new Product { Id = 3, Name = "P3", Price = 300m, Currency = "TRY", IsActive = true } },
            new() { Id = 2, ProductId = 2, WishlistId = 10, AddedAt = addedAt.AddMinutes(-1), AddedAtPrice = 200m, Product = new Product { Id = 2, Name = "P2", Price = 200m, Currency = "TRY", IsActive = true } },
            new() { Id = 1, ProductId = 1, WishlistId = 10, AddedAt = addedAt.AddMinutes(-2), AddedAtPrice = 100m, Product = new Product { Id = 1, Name = "P1", Price = 100m, Currency = "TRY", IsActive = true } }
        };

        _wishlistDalMock
            .Setup(d => d.GetAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Wishlist, bool>>>()))
            .ReturnsAsync(wishlist);

        _wishlistItemDalMock
            .Setup(d => d.GetPagedByWishlistIdAsync(wishlist.Id, null, null, 3, null))
            .ReturnsAsync(items);
        _mapperMock
            .Setup(m => m.ToWishlistItemDto(It.IsAny<WishlistItem>()))
            .Returns((WishlistItem item) => new WishlistItemDto
            {
                Id = item.Id,
                ProductId = item.ProductId,
                ProductName = item.Product?.Name ?? string.Empty,
                ProductPrice = item.Product?.Price ?? item.AddedAtPrice,
                ProductCurrency = item.Product?.Currency ?? "TRY",
                AddedAt = item.AddedAt,
                AddedAtPrice = item.AddedAtPrice,
                IsAvailable = item.Product?.IsActive == true
            });

        var result = await _manager.GetWishlistByUserIdAsync(userId, limit: 2);

        result.Success.Should().BeTrue();
        result.Data.Items.Should().HaveCount(2);
        result.Data.HasMore.Should().BeTrue();
        result.Data.NextCursor.Should().NotBeNullOrWhiteSpace();
        result.Data.Limit.Should().Be(2);
    }

    [Fact]
    public async Task CreateCollectionAsync_WhenNameIsUnique_CreatesCollection()
    {
        const int userId = 1;
        var wishlist = new Wishlist { Id = 10, UserId = userId };

        _wishlistDalMock.Setup(x => x.GetOrCreateByUserIdAsync(userId))
            .ReturnsAsync(wishlist);
        _wishlistCollectionDalMock.Setup(x => x.GetOrCreateDefaultCollectionAsync(wishlist.Id))
            .ReturnsAsync(new WishlistCollection { Id = 1010, WishlistId = wishlist.Id, Name = "Favorilerim", IsDefault = true });
        _wishlistCollectionDalMock.Setup(x => x.ExistsByNameAsync(wishlist.Id, "Hediyeler"))
            .ReturnsAsync(false);
        _wishlistCollectionDalMock.Setup(x => x.AddAsync(It.IsAny<WishlistCollection>()))
            .Callback<WishlistCollection>(collection => collection.Id = 2020)
            .ReturnsAsync((WishlistCollection collection) => collection);

        var result = await _manager.CreateCollectionAsync(userId, new CreateWishlistCollectionRequest { Name = "Hediyeler" });

        result.Success.Should().BeTrue();
        result.Data.Id.Should().Be(2020);
        result.Data.Name.Should().Be("Hediyeler");
        _uowMock.Verify(x => x.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task MoveItemToCollectionAsync_WhenCollectionExists_MovesItem()
    {
        const int userId = 1;
        const int productId = 4;
        const int targetCollectionId = 22;
        var wishlist = new Wishlist { Id = 10, UserId = userId };

        _wishlistDalMock.Setup(x => x.GetAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Wishlist, bool>>>()))
            .ReturnsAsync(wishlist);
        _wishlistCollectionDalMock.Setup(x => x.GetAsync(It.IsAny<System.Linq.Expressions.Expression<Func<WishlistCollection, bool>>>()))
            .ReturnsAsync(new WishlistCollection { Id = targetCollectionId, WishlistId = wishlist.Id, Name = "Teknoloji" });
        _wishlistItemDalMock.Setup(x => x.GetAsync(It.IsAny<System.Linq.Expressions.Expression<Func<WishlistItem, bool>>>()))
            .ReturnsAsync(new WishlistItem
            {
                Id = 1,
                WishlistId = wishlist.Id,
                ProductId = productId,
                CollectionId = 11
            });
        _wishlistItemDalMock.Setup(x => x.MoveToCollectionAsync(wishlist.Id, productId, targetCollectionId))
            .ReturnsAsync(1);

        var result = await _manager.MoveItemToCollectionAsync(userId, productId, targetCollectionId);

        result.Success.Should().BeTrue();
        _wishlistItemDalMock.Verify(x => x.MoveToCollectionAsync(wishlist.Id, productId, targetCollectionId), Times.Once);
    }

    [Fact]
    public async Task AddAvailableItemsToCartAsync_ReturnsAddedAndSkippedSummary()
    {
        const int userId = 1;
        var wishlist = new Wishlist { Id = 10, UserId = userId };
        var wishlistItems = new List<WishlistItem>
        {
            new() { Id = 1, WishlistId = 10, ProductId = 1 },
            new() { Id = 2, WishlistId = 10, ProductId = 2 },
            new() { Id = 3, WishlistId = 10, ProductId = 3 }
        };
        var products = new List<Product>
        {
            new() { Id = 1, Name = "P1", IsActive = true, Inventory = new Inventory { QuantityAvailable = 5 } },
            new() { Id = 2, Name = "P2", IsActive = false, Inventory = new Inventory { QuantityAvailable = 5 } },
            new() { Id = 3, Name = "P3", IsActive = true, Inventory = new Inventory { QuantityAvailable = 0 } }
        };

        _wishlistDalMock
            .Setup(x => x.GetAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Wishlist, bool>>>()))
            .ReturnsAsync(wishlist);
        _wishlistItemDalMock
            .Setup(x => x.GetListAsync(It.IsAny<System.Linq.Expressions.Expression<Func<WishlistItem, bool>>>()))
            .ReturnsAsync(wishlistItems);
        _productDalMock
            .Setup(x => x.GetByIdsWithInventoryAsync(It.IsAny<List<int>>()))
            .ReturnsAsync(products);
        _cartCacheServiceMock
            .Setup(x => x.GetCartItemsAsync(userId))
            .ReturnsAsync(new Dictionary<int, int>());

        var result = await _manager.AddAvailableItemsToCartAsync(userId);

        result.Success.Should().BeTrue();
        result.Data.RequestedCount.Should().Be(3);
        result.Data.AddedCount.Should().Be(1);
        result.Data.SkippedCount.Should().Be(2);
        result.Message.Should().Contain("1 ürün sepete eklendi");
        _cartCacheServiceMock.Verify(x => x.IncrementItemQuantityAsync(userId, 1, 1), Times.Once);
    }
}
