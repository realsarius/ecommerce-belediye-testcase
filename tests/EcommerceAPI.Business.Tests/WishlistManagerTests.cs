using EcommerceAPI.Business.Concrete;
using EcommerceAPI.Business.Constants;
using EcommerceAPI.Business.Mappers;
using EcommerceAPI.DataAccess.Abstract;
using EcommerceAPI.Entities.Concrete;
using EcommerceAPI.Entities.DTOs;
using Moq;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace EcommerceAPI.Business.Tests;

[TestFixture]
public class WishlistManagerTests
{
    private Mock<IWishlistDal> _mockWishlistDal;
    private Mock<IWishlistItemDal> _mockWishlistItemDal;
    private Mock<IProductDal> _mockProductDal;
    private Mock<IWishlistMapper> _mockWishlistMapper;
    private WishlistManager _wishlistManager;

    [SetUp]
    public void Setup()
    {
        _mockWishlistDal = new Mock<IWishlistDal>();
        _mockWishlistItemDal = new Mock<IWishlistItemDal>();
        _mockProductDal = new Mock<IProductDal>();
        _mockWishlistMapper = new Mock<IWishlistMapper>();

        _wishlistManager = new WishlistManager(
            _mockWishlistDal.Object,
            _mockWishlistItemDal.Object,
            _mockProductDal.Object,
            _mockWishlistMapper.Object
        );
    }

    [Test]
    public async Task AddItemToWishlistAsync_ProductNotFound_ReturnsError()
    {
        // Arrange
        _mockProductDal.Setup(d => d.GetAsync(It.IsAny<Expression<System.Func<Product, bool>>>(), null))
            .ReturnsAsync((Product)null!);

        // Act
        var result = await _wishlistManager.AddItemToWishlistAsync(1, 99);

        // Assert
        Assert.IsFalse(result.Success);
        Assert.AreEqual(Messages.ProductNotFound, result.Message);
    }

    [Test]
    public async Task AddItemToWishlistAsync_Success_AddsItem()
    {
        // Arrange
        _mockProductDal.Setup(d => d.GetAsync(It.IsAny<Expression<System.Func<Product, bool>>>(), null))
            .ReturnsAsync(new Product { Id = 1 });

        _mockWishlistDal.Setup(d => d.GetAsync(It.IsAny<Expression<System.Func<Wishlist, bool>>>(), null))
            .ReturnsAsync(new Wishlist { Id = 1, UserId = 1 });

        _mockWishlistItemDal.Setup(d => d.GetAsync(It.IsAny<Expression<System.Func<WishlistItem, bool>>>(), null))
            .ReturnsAsync((WishlistItem)null!); // Item doesn't exist yet

        // Act
        var result = await _wishlistManager.AddItemToWishlistAsync(1, 1);

        // Assert
        Assert.IsTrue(result.Success);
        _mockWishlistItemDal.Verify(d => d.AddAsync(It.Is<WishlistItem>(w => w.ProductId == 1 && w.WishlistId == 1)), Times.Once);
    }
}
