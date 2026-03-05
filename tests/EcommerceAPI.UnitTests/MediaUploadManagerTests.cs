using EcommerceAPI.Business.Concrete;
using EcommerceAPI.Core.Interfaces;
using EcommerceAPI.DataAccess.Abstract;
using EcommerceAPI.Entities.Concrete;
using EcommerceAPI.Entities.DTOs;
using FluentAssertions;
using Moq;

namespace EcommerceAPI.UnitTests;

public class MediaUploadManagerTests
{
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
}
