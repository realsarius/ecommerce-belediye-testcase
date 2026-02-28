using FluentAssertions;
using Moq;
using EcommerceAPI.Business.Concrete;
using EcommerceAPI.Business.Mappers;
using EcommerceAPI.Core.Interfaces;
using EcommerceAPI.DataAccess.Abstract;
using EcommerceAPI.Entities.Concrete;
using EcommerceAPI.Entities.DTOs;

namespace EcommerceAPI.UnitTests;

public class WishlistManagerTests
{
    private readonly Mock<IWishlistDal> _wishlistDalMock;
    private readonly Mock<IWishlistItemDal> _wishlistItemDalMock;
    private readonly Mock<IProductDal> _productDalMock;
    private readonly Mock<IWishlistMapper> _mapperMock;
    private readonly Mock<IUnitOfWork> _uowMock;
    private readonly WishlistManager _manager;

    public WishlistManagerTests()
    {
        _wishlistDalMock = new Mock<IWishlistDal>();
        _wishlistItemDalMock = new Mock<IWishlistItemDal>();
        _productDalMock = new Mock<IProductDal>();
        _mapperMock = new Mock<IWishlistMapper>();
        _uowMock = new Mock<IUnitOfWork>();

        _manager = new WishlistManager(
            _wishlistDalMock.Object,
            _wishlistItemDalMock.Object,
            _productDalMock.Object,
            _mapperMock.Object,
            _uowMock.Object
        );
    }

    [Fact]
    public async Task GetWishlistByUserIdAsync_WhenNoWishlistExists_ReturnsEmptyWishlist()
    {
        int userId = 1;
        _wishlistDalMock.Setup(d => d.GetAsync(It.IsAny<System.Linq.Expressions.Expression<System.Func<Wishlist, bool>>>()))
            .ReturnsAsync((Wishlist?)null);

        var result = await _manager.GetWishlistByUserIdAsync(userId);

        result.Success.Should().BeTrue();
        result.Data.UserId.Should().Be(userId);
        result.Data.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task AddItemToWishlistAsync_WhenProductDoesNotExist_ReturnsError()
    {
        int userId = 1;
        int productId = 99;

        _productDalMock.Setup(d => d.GetAsync(It.IsAny<System.Linq.Expressions.Expression<System.Func<Product, bool>>>()))
            .ReturnsAsync((Product?)null);

        var result = await _manager.AddItemToWishlistAsync(userId, productId);

        result.Success.Should().BeFalse();
        result.Message.Should().Be(EcommerceAPI.Business.Constants.Messages.ProductNotFound);
    }

    [Fact]
    public async Task AddItemToWishlistAsync_WhenProductValidAndNew_AddsItemAndSaves()
    {
        int userId = 1;
        int productId = 2;
        decimal productPrice = 299.99m;
        var existingWishlist = new Wishlist { Id = 10, UserId = userId };

        _productDalMock.Setup(d => d.GetAsync(It.IsAny<System.Linq.Expressions.Expression<System.Func<Product, bool>>>()))
            .ReturnsAsync(new Product { Id = productId, Price = productPrice, IsActive = true });

        _wishlistDalMock.Setup(d => d.GetAsync(It.IsAny<System.Linq.Expressions.Expression<System.Func<Wishlist, bool>>>()))
            .ReturnsAsync(existingWishlist);

        _wishlistItemDalMock.Setup(d => d.CountAsync(It.IsAny<System.Linq.Expressions.Expression<System.Func<WishlistItem, bool>>>()))
            .ReturnsAsync(0);

        _wishlistItemDalMock.Setup(d => d.GetListAsync(It.IsAny<System.Linq.Expressions.Expression<System.Func<WishlistItem, bool>>>()))
            .ReturnsAsync(new List<WishlistItem>());

        var result = await _manager.AddItemToWishlistAsync(userId, productId);

        result.Success.Should().BeTrue();
        _wishlistItemDalMock.Verify(d => d.AddAsync(It.Is<WishlistItem>(i =>
            i.ProductId == productId &&
            i.WishlistId == 10 &&
            i.AddedAtPrice == productPrice)), Times.Once);
        _uowMock.Verify(u => u.SaveChangesAsync(), Times.AtLeastOnce);
    }

    [Fact]
    public async Task AddItemToWishlistAsync_WhenProductInactive_ReturnsError()
    {
        int userId = 1;
        int productId = 2;

        _productDalMock.Setup(d => d.GetAsync(It.IsAny<System.Linq.Expressions.Expression<System.Func<Product, bool>>>()))
            .ReturnsAsync(new Product { Id = productId, IsActive = false });

        var result = await _manager.AddItemToWishlistAsync(userId, productId);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("satışta değil");
    }

    [Fact]
    public async Task AddItemToWishlistAsync_WhenWishlistLimitReached_ReturnsError()
    {
        int userId = 1;
        int productId = 2;
        var existingWishlist = new Wishlist { Id = 10, UserId = userId };

        _productDalMock.Setup(d => d.GetAsync(It.IsAny<System.Linq.Expressions.Expression<System.Func<Product, bool>>>()))
            .ReturnsAsync(new Product { Id = productId, Price = 100m, IsActive = true });

        _wishlistDalMock.Setup(d => d.GetAsync(It.IsAny<System.Linq.Expressions.Expression<System.Func<Wishlist, bool>>>()))
            .ReturnsAsync(existingWishlist);

        _wishlistItemDalMock.Setup(d => d.CountAsync(It.IsAny<System.Linq.Expressions.Expression<System.Func<WishlistItem, bool>>>()))
            .ReturnsAsync(500);

        var result = await _manager.AddItemToWishlistAsync(userId, productId);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("maksimum kapasiteye");
    }

    [Fact]
    public async Task GetWishlistByUserIdAsync_FiltersInactiveProducts()
    {
        int userId = 1;
        var wishlist = new Wishlist { Id = 10, UserId = userId };
        var activeProduct = new Product { Id = 1, Name = "Active", IsActive = true, Price = 100m };
        var inactiveProduct = new Product { Id = 2, Name = "Inactive", IsActive = false, Price = 200m };

        var items = new List<WishlistItem>
        {
            new WishlistItem { Id = 1, ProductId = 1, WishlistId = 10, AddedAtPrice = 100m, AddedAt = DateTime.UtcNow },
            new WishlistItem { Id = 2, ProductId = 2, WishlistId = 10, AddedAtPrice = 200m, AddedAt = DateTime.UtcNow }
        };

        _wishlistDalMock.Setup(d => d.GetAsync(It.IsAny<System.Linq.Expressions.Expression<System.Func<Wishlist, bool>>>()))
            .ReturnsAsync(wishlist);

        _wishlistItemDalMock.Setup(d => d.GetListAsync(It.IsAny<System.Linq.Expressions.Expression<System.Func<WishlistItem, bool>>>()))
            .ReturnsAsync(items);

        _productDalMock.Setup(d => d.GetAsync(It.Is<System.Linq.Expressions.Expression<System.Func<Product, bool>>>(e => true)))
            .ReturnsAsync((System.Linq.Expressions.Expression<System.Func<Product, bool>> predicate) =>
            {
                var compiled = predicate.Compile();
                return new[] { activeProduct, inactiveProduct }.FirstOrDefault(p => compiled(p));
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
        result.Data.Items.Should().HaveCount(1);
        result.Data.Items.First().ProductId.Should().Be(1);
    }
}
