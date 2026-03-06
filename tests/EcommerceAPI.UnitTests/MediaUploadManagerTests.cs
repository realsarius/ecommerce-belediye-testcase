using EcommerceAPI.Business.Concrete;
using EcommerceAPI.Core.Interfaces;
using EcommerceAPI.Application.Abstractions.Persistence;
using EcommerceAPI.Entities.Concrete;
using EcommerceAPI.Entities.DTOs;
using FluentAssertions;
using Moq;

namespace EcommerceAPI.UnitTests;

public class MediaUploadManagerTests
{
    [Fact]
    public async Task GetPresignedUploadUrlAsync_WhenNonAdminRequestsCategoryContext_ShouldReturnAuthorizationError()
    {
        var objectStorageMock = new Mock<IObjectStorageService>();
        var productDalMock = new Mock<IProductDal>();
        var categoryDalMock = new Mock<ICategoryDal>();
        var sellerProfileDalMock = new Mock<ISellerProfileDal>();
        var unitOfWorkMock = new Mock<IUnitOfWork>();
        var loggerMock = new Mock<Microsoft.Extensions.Logging.ILogger<MediaUploadManager>>();

        var manager = new MediaUploadManager(
            objectStorageMock.Object,
            productDalMock.Object,
            categoryDalMock.Object,
            sellerProfileDalMock.Object,
            unitOfWorkMock.Object,
            Mock.Of<MassTransit.IPublishEndpoint>(),
            loggerMock.Object);

        var result = await manager.GetPresignedUploadUrlAsync(1, false, new PresignMediaUploadRequest
        {
            Context = "category",
            ReferenceId = 7,
            ContentType = "image/png",
            FileSizeBytes = 1024
        });

        result.Success.Should().BeFalse();
        result.Message.Should().Be("Yetkiniz yok");
    }

    [Fact]
    public async Task ConfirmUploadAsync_WhenObjectKeyFormatIsInvalid_ShouldReturnError()
    {
        var objectStorageMock = new Mock<IObjectStorageService>();
        var productDalMock = new Mock<IProductDal>();
        var categoryDalMock = new Mock<ICategoryDal>();
        var sellerProfileDalMock = new Mock<ISellerProfileDal>();
        var unitOfWorkMock = new Mock<IUnitOfWork>();
        var loggerMock = new Mock<Microsoft.Extensions.Logging.ILogger<MediaUploadManager>>();

        var manager = new MediaUploadManager(
            objectStorageMock.Object,
            productDalMock.Object,
            categoryDalMock.Object,
            sellerProfileDalMock.Object,
            unitOfWorkMock.Object,
            Mock.Of<MassTransit.IPublishEndpoint>(),
            loggerMock.Object);

        var result = await manager.ConfirmUploadAsync(1, true, new ConfirmMediaUploadRequest
        {
            Context = "category",
            ReferenceId = 7,
            ObjectKey = "categories/category-7/../attack.png"
        });

        result.Success.Should().BeFalse();
        result.Message.Should().Be("Geçersiz object key formatı");
        objectStorageMock.Verify(service => service.ExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ConfirmUploadAsync_WhenImageSignatureIsInvalid_ShouldReturnError()
    {
        var objectStorageMock = new Mock<IObjectStorageService>();
        objectStorageMock
            .Setup(service => service.ExistsAsync("categories/category-7/test.png", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        objectStorageMock
            .Setup(service => service.GetObjectHeaderBytesAsync("categories/category-7/test.png", 32, It.IsAny<CancellationToken>()))
            .ReturnsAsync([0x48, 0x65, 0x6C, 0x6C, 0x6F]);

        var productDalMock = new Mock<IProductDal>();
        var categoryDalMock = new Mock<ICategoryDal>();
        var sellerProfileDalMock = new Mock<ISellerProfileDal>();
        var unitOfWorkMock = new Mock<IUnitOfWork>();
        var loggerMock = new Mock<Microsoft.Extensions.Logging.ILogger<MediaUploadManager>>();

        var manager = new MediaUploadManager(
            objectStorageMock.Object,
            productDalMock.Object,
            categoryDalMock.Object,
            sellerProfileDalMock.Object,
            unitOfWorkMock.Object,
            Mock.Of<MassTransit.IPublishEndpoint>(),
            loggerMock.Object);

        var result = await manager.ConfirmUploadAsync(1, true, new ConfirmMediaUploadRequest
        {
            Context = "category",
            ReferenceId = 7,
            ObjectKey = "categories/category-7/test.png"
        });

        result.Success.Should().BeFalse();
        result.Message.Should().Be("Yüklenen dosya geçerli bir görsel değil");
        categoryDalMock.Verify(dal => dal.GetAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Category, bool>>>()), Times.Never);
    }

    [Fact]
    public async Task ConfirmUploadAsync_WhenImageSignatureIsValid_ShouldPersistCategoryImage()
    {
        var category = new Category
        {
            Id = 7,
            Name = "Elektronik",
            Description = "Kategori"
        };

        var objectStorageMock = new Mock<IObjectStorageService>();
        objectStorageMock
            .Setup(service => service.ExistsAsync("categories/category-7/test.png", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        objectStorageMock
            .Setup(service => service.GetObjectHeaderBytesAsync("categories/category-7/test.png", 32, It.IsAny<CancellationToken>()))
            .ReturnsAsync([0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]);
        objectStorageMock
            .Setup(service => service.GetPublicUrl("categories/category-7/test.png"))
            .Returns("https://img.example.com/categories/category-7/test.png");

        var productDalMock = new Mock<IProductDal>();
        var categoryDalMock = new Mock<ICategoryDal>();
        categoryDalMock
            .Setup(dal => dal.GetAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Category, bool>>>()))
            .ReturnsAsync(category);

        var sellerProfileDalMock = new Mock<ISellerProfileDal>();
        var unitOfWorkMock = new Mock<IUnitOfWork>();
        unitOfWorkMock.Setup(unit => unit.SaveChangesAsync()).ReturnsAsync(1);
        var loggerMock = new Mock<Microsoft.Extensions.Logging.ILogger<MediaUploadManager>>();

        var manager = new MediaUploadManager(
            objectStorageMock.Object,
            productDalMock.Object,
            categoryDalMock.Object,
            sellerProfileDalMock.Object,
            unitOfWorkMock.Object,
            Mock.Of<MassTransit.IPublishEndpoint>(),
            loggerMock.Object);

        var result = await manager.ConfirmUploadAsync(1, true, new ConfirmMediaUploadRequest
        {
            Context = "category",
            ReferenceId = 7,
            ObjectKey = "categories/category-7/test.png"
        });

        result.Success.Should().BeTrue();
        result.Data.ImageUrl.Should().Be("https://img.example.com/categories/category-7/test.png");
        result.Data.ObjectKey.Should().Be("categories/category-7/test.png");

        categoryDalMock.Verify(dal => dal.Update(It.Is<Category>(item => item.Id == 7 && item.ImageObjectKey == "categories/category-7/test.png")), Times.Once);
        unitOfWorkMock.Verify(unit => unit.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task DeleteProductImageAsync_WhenSellerDoesNotOwnProduct_ShouldReturnAuthorizationError()
    {
        var product = new Product
        {
            Id = 99,
            Name = "Test Product",
            Description = "Desc",
            SKU = "SKU-1",
            CategoryId = 1,
            SellerId = 10,
            Images =
            [
                new ProductImage
                {
                    Id = 55,
                    ProductId = 99,
                    ImageUrl = "https://img.example.com/a.webp",
                    ObjectKey = "products/seller-10/product-99/a.webp",
                    IsPrimary = true,
                    SortOrder = 0
                }
            ]
        };

        var objectStorageMock = new Mock<IObjectStorageService>();
        var productDalMock = new Mock<IProductDal>();
        productDalMock
            .Setup(dal => dal.GetByImageIdForUpdateAsync(55))
            .ReturnsAsync(product);

        var categoryDalMock = new Mock<ICategoryDal>();
        var sellerProfileDalMock = new Mock<ISellerProfileDal>();
        sellerProfileDalMock
            .Setup(dal => dal.GetAsync(It.IsAny<System.Linq.Expressions.Expression<Func<SellerProfile, bool>>>()))
            .ReturnsAsync(new SellerProfile { Id = 11, UserId = 2, BrandName = "Other Seller" });

        var unitOfWorkMock = new Mock<IUnitOfWork>();
        var loggerMock = new Mock<Microsoft.Extensions.Logging.ILogger<MediaUploadManager>>();

        var manager = new MediaUploadManager(
            objectStorageMock.Object,
            productDalMock.Object,
            categoryDalMock.Object,
            sellerProfileDalMock.Object,
            unitOfWorkMock.Object,
            Mock.Of<MassTransit.IPublishEndpoint>(),
            loggerMock.Object);

        var result = await manager.DeleteProductImageAsync(userId: 2, isAdmin: false, imageId: 55);

        result.Success.Should().BeFalse();
        result.Message.Should().Be("Yetkiniz yok");
        objectStorageMock.Verify(service => service.DeleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        unitOfWorkMock.Verify(unit => unit.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task ReorderProductImagesAsync_WhenSellerDoesNotOwnProduct_ShouldReturnAuthorizationError()
    {
        var product = new Product
        {
            Id = 99,
            Name = "Test Product",
            Description = "Desc",
            SKU = "SKU-1",
            CategoryId = 1,
            SellerId = 10,
            Images =
            [
                new ProductImage
                {
                    Id = 1,
                    ProductId = 99,
                    ImageUrl = "https://img.example.com/a.webp",
                    IsPrimary = true,
                    SortOrder = 0
                },
                new ProductImage
                {
                    Id = 2,
                    ProductId = 99,
                    ImageUrl = "https://img.example.com/b.webp",
                    IsPrimary = false,
                    SortOrder = 1
                }
            ]
        };

        var objectStorageMock = new Mock<IObjectStorageService>();
        var productDalMock = new Mock<IProductDal>();
        productDalMock
            .Setup(dal => dal.GetByIdForUpdateAsync(99))
            .ReturnsAsync(product);

        var categoryDalMock = new Mock<ICategoryDal>();
        var sellerProfileDalMock = new Mock<ISellerProfileDal>();
        sellerProfileDalMock
            .Setup(dal => dal.GetAsync(It.IsAny<System.Linq.Expressions.Expression<Func<SellerProfile, bool>>>()))
            .ReturnsAsync(new SellerProfile { Id = 11, UserId = 2, BrandName = "Other Seller" });

        var unitOfWorkMock = new Mock<IUnitOfWork>();
        var loggerMock = new Mock<Microsoft.Extensions.Logging.ILogger<MediaUploadManager>>();

        var manager = new MediaUploadManager(
            objectStorageMock.Object,
            productDalMock.Object,
            categoryDalMock.Object,
            sellerProfileDalMock.Object,
            unitOfWorkMock.Object,
            Mock.Of<MassTransit.IPublishEndpoint>(),
            loggerMock.Object);

        var result = await manager.ReorderProductImagesAsync(userId: 2, isAdmin: false, productId: 99, new ReorderProductImagesRequest
        {
            ImageOrders =
            [
                new ReorderProductImageItemRequest { ImageId = 1, DisplayOrder = 0, IsPrimary = true },
                new ReorderProductImageItemRequest { ImageId = 2, DisplayOrder = 1, IsPrimary = false }
            ]
        });

        result.Success.Should().BeFalse();
        result.Message.Should().Be("Yetkiniz yok");
        unitOfWorkMock.Verify(unit => unit.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task ReorderProductImagesAsync_WhenRequestIsValid_ShouldUpdateOrderAndPrimaryInSingleSave()
    {
        var product = new Product
        {
            Id = 99,
            Name = "Test Product",
            Description = "Desc",
            SKU = "SKU-1",
            CategoryId = 1,
            SellerId = 10,
            Images =
            [
                new ProductImage
                {
                    Id = 1,
                    ProductId = 99,
                    ImageUrl = "https://img.example.com/a.webp",
                    IsPrimary = true,
                    SortOrder = 0
                },
                new ProductImage
                {
                    Id = 2,
                    ProductId = 99,
                    ImageUrl = "https://img.example.com/b.webp",
                    IsPrimary = false,
                    SortOrder = 1
                }
            ]
        };

        var objectStorageMock = new Mock<IObjectStorageService>();
        var productDalMock = new Mock<IProductDal>();
        productDalMock
            .Setup(dal => dal.GetByIdForUpdateAsync(99))
            .ReturnsAsync(product);

        var categoryDalMock = new Mock<ICategoryDal>();
        var sellerProfileDalMock = new Mock<ISellerProfileDal>();
        sellerProfileDalMock
            .Setup(dal => dal.GetAsync(It.IsAny<System.Linq.Expressions.Expression<Func<SellerProfile, bool>>>()))
            .ReturnsAsync(new SellerProfile { Id = 10, UserId = 2, BrandName = "Owner Seller" });

        var unitOfWorkMock = new Mock<IUnitOfWork>();
        unitOfWorkMock.Setup(unit => unit.SaveChangesAsync()).ReturnsAsync(1);
        var loggerMock = new Mock<Microsoft.Extensions.Logging.ILogger<MediaUploadManager>>();

        var manager = new MediaUploadManager(
            objectStorageMock.Object,
            productDalMock.Object,
            categoryDalMock.Object,
            sellerProfileDalMock.Object,
            unitOfWorkMock.Object,
            Mock.Of<MassTransit.IPublishEndpoint>(),
            loggerMock.Object);

        var result = await manager.ReorderProductImagesAsync(userId: 2, isAdmin: false, productId: 99, new ReorderProductImagesRequest
        {
            ImageOrders =
            [
                new ReorderProductImageItemRequest { ImageId = 2, DisplayOrder = 0, IsPrimary = true },
                new ReorderProductImageItemRequest { ImageId = 1, DisplayOrder = 1, IsPrimary = false }
            ]
        });

        result.Success.Should().BeTrue();
        product.Images.Single(image => image.Id == 2).SortOrder.Should().Be(0);
        product.Images.Single(image => image.Id == 2).IsPrimary.Should().BeTrue();
        product.Images.Single(image => image.Id == 1).SortOrder.Should().Be(1);
        product.Images.Single(image => image.Id == 1).IsPrimary.Should().BeFalse();
        productDalMock.Verify(dal => dal.Update(product), Times.Once);
        unitOfWorkMock.Verify(unit => unit.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task DeleteProductImageAsync_WhenPrimaryImageDeleted_ShouldAssignFallbackPrimaryAndSort()
    {
        var product = new Product
        {
            Id = 99,
            Name = "Test Product",
            Description = "Desc",
            SKU = "SKU-1",
            CategoryId = 1,
            SellerId = 10,
            Images =
            [
                new ProductImage
                {
                    Id = 55,
                    ProductId = 99,
                    ImageUrl = "https://img.example.com/a.webp",
                    ObjectKey = "products/seller-10/product-99/a.webp",
                    IsPrimary = true,
                    SortOrder = 0
                },
                new ProductImage
                {
                    Id = 56,
                    ProductId = 99,
                    ImageUrl = "https://img.example.com/b.webp",
                    ObjectKey = "products/seller-10/product-99/b.webp",
                    IsPrimary = false,
                    SortOrder = 5
                }
            ]
        };

        var objectStorageMock = new Mock<IObjectStorageService>();
        var productDalMock = new Mock<IProductDal>();
        productDalMock
            .Setup(dal => dal.GetByImageIdForUpdateAsync(55))
            .ReturnsAsync(product);

        var categoryDalMock = new Mock<ICategoryDal>();
        var sellerProfileDalMock = new Mock<ISellerProfileDal>();
        sellerProfileDalMock
            .Setup(dal => dal.GetAsync(It.IsAny<System.Linq.Expressions.Expression<Func<SellerProfile, bool>>>()))
            .ReturnsAsync(new SellerProfile { Id = 10, UserId = 2, BrandName = "Owner Seller" });

        var unitOfWorkMock = new Mock<IUnitOfWork>();
        unitOfWorkMock.Setup(unit => unit.SaveChangesAsync()).ReturnsAsync(1);
        var loggerMock = new Mock<Microsoft.Extensions.Logging.ILogger<MediaUploadManager>>();

        var manager = new MediaUploadManager(
            objectStorageMock.Object,
            productDalMock.Object,
            categoryDalMock.Object,
            sellerProfileDalMock.Object,
            unitOfWorkMock.Object,
            Mock.Of<MassTransit.IPublishEndpoint>(),
            loggerMock.Object);

        var result = await manager.DeleteProductImageAsync(userId: 2, isAdmin: false, imageId: 55);

        result.Success.Should().BeTrue();
        product.Images.Should().HaveCount(1);
        product.Images.Single().Id.Should().Be(56);
        product.Images.Single().IsPrimary.Should().BeTrue();
        product.Images.Single().SortOrder.Should().Be(0);
        objectStorageMock.Verify(service => service.DeleteAsync("products/seller-10/product-99/a.webp", It.IsAny<CancellationToken>()), Times.Once);
        unitOfWorkMock.Verify(unit => unit.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task GetPresignedUploadUrlAsync_WhenProductSellerIsMissing_ShouldReturnError()
    {
        var product = new Product
        {
            Id = 99,
            Name = "Test Product",
            Description = "Desc",
            SKU = "SKU-1",
            CategoryId = 1,
            SellerId = null
        };

        var objectStorageMock = new Mock<IObjectStorageService>();
        var productDalMock = new Mock<IProductDal>();
        productDalMock
            .Setup(dal => dal.GetByIdWithDetailsAsync(99))
            .ReturnsAsync(product);

        var categoryDalMock = new Mock<ICategoryDal>();
        var sellerProfileDalMock = new Mock<ISellerProfileDal>();
        var unitOfWorkMock = new Mock<IUnitOfWork>();
        var loggerMock = new Mock<Microsoft.Extensions.Logging.ILogger<MediaUploadManager>>();

        var manager = new MediaUploadManager(
            objectStorageMock.Object,
            productDalMock.Object,
            categoryDalMock.Object,
            sellerProfileDalMock.Object,
            unitOfWorkMock.Object,
            Mock.Of<MassTransit.IPublishEndpoint>(),
            loggerMock.Object);

        var result = await manager.GetPresignedUploadUrlAsync(1, true, new PresignMediaUploadRequest
        {
            Context = "product",
            ReferenceId = 99,
            ContentType = "image/webp",
            FileSizeBytes = 1024
        });

        result.Success.Should().BeFalse();
        result.Message.Should().Be("Ürün satıcı bilgisi eksik olduğu için yükleme başlatılamadı");
        objectStorageMock.Verify(service => service.GeneratePresignedUploadUrlAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ConfirmUploadAsync_WhenProductSellerIsMissing_ShouldReturnError()
    {
        var product = new Product
        {
            Id = 99,
            Name = "Test Product",
            Description = "Desc",
            SKU = "SKU-1",
            CategoryId = 1,
            SellerId = null
        };

        var objectStorageMock = new Mock<IObjectStorageService>();
        objectStorageMock
            .Setup(service => service.ExistsAsync("products/seller-0/product-99/test.webp", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        objectStorageMock
            .Setup(service => service.GetObjectHeaderBytesAsync("products/seller-0/product-99/test.webp", 32, It.IsAny<CancellationToken>()))
            .ReturnsAsync([0x52, 0x49, 0x46, 0x46, 0x00, 0x00, 0x00, 0x00, 0x57, 0x45, 0x42, 0x50]);

        var productDalMock = new Mock<IProductDal>();
        productDalMock
            .Setup(dal => dal.GetByIdForUpdateAsync(99))
            .ReturnsAsync(product);

        var categoryDalMock = new Mock<ICategoryDal>();
        var sellerProfileDalMock = new Mock<ISellerProfileDal>();
        var unitOfWorkMock = new Mock<IUnitOfWork>();
        var loggerMock = new Mock<Microsoft.Extensions.Logging.ILogger<MediaUploadManager>>();

        var manager = new MediaUploadManager(
            objectStorageMock.Object,
            productDalMock.Object,
            categoryDalMock.Object,
            sellerProfileDalMock.Object,
            unitOfWorkMock.Object,
            Mock.Of<MassTransit.IPublishEndpoint>(),
            loggerMock.Object);

        var result = await manager.ConfirmUploadAsync(1, true, new ConfirmMediaUploadRequest
        {
            Context = "product",
            ReferenceId = 99,
            ObjectKey = "products/seller-0/product-99/test.webp"
        });

        result.Success.Should().BeFalse();
        result.Message.Should().Be("Ürün satıcı bilgisi eksik olduğu için görsel kaydı yapılamadı");
        unitOfWorkMock.Verify(unit => unit.SaveChangesAsync(), Times.Never);
    }
}
