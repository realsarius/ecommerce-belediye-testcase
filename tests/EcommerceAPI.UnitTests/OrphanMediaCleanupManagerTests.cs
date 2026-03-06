using EcommerceAPI.Business.Concrete;
using EcommerceAPI.Core.Interfaces;
using EcommerceAPI.Application.Abstractions.Persistence;
using EcommerceAPI.Entities.Concrete;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Moq;

namespace EcommerceAPI.UnitTests;

public class OrphanMediaCleanupManagerTests
{
    [Fact]
    public async Task ExecuteAsync_WhenOrphanObjectsExist_ShouldDeleteOnlyOrphansOlderThanGrace()
    {
        var now = DateTime.UtcNow;
        var keeperKey = "products/seller-1/product-10/keep.webp";
        var orphanKey = "products/seller-1/product-10/orphan.webp";

        var objectStorageMock = new Mock<IObjectStorageService>();
        objectStorageMock
            .Setup(service => service.ListObjectsAsync(null, It.IsAny<int?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new ObjectStorageObjectInfo(keeperKey, now.AddHours(-48)),
                new ObjectStorageObjectInfo(orphanKey, now.AddHours(-30)),
                new ObjectStorageObjectInfo("products/seller-1/product-10/new.webp", now.AddHours(-1))
            ]);

        var productDalMock = new Mock<IProductDal>();
        productDalMock
            .Setup(dal => dal.GetAllImageObjectKeysAsync())
            .ReturnsAsync([keeperKey]);

        var categoryDalMock = new Mock<ICategoryDal>();
        categoryDalMock
            .Setup(dal => dal.GetListAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Category, bool>>>()))
            .ReturnsAsync([]);

        var sellerProfileDalMock = new Mock<ISellerProfileDal>();
        sellerProfileDalMock
            .Setup(dal => dal.GetListAsync(It.IsAny<System.Linq.Expressions.Expression<Func<SellerProfile, bool>>>()))
            .ReturnsAsync([]);

        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["CloudflareR2:OrphanCleanupGraceHours"] = "24",
            ["CloudflareR2:OrphanCleanupMaxScanPerRun"] = "1000",
            ["CloudflareR2:OrphanCleanupMaxDeletePerRun"] = "1000"
        });

        var loggerMock = new Mock<Microsoft.Extensions.Logging.ILogger<OrphanMediaCleanupManager>>();

        var manager = new OrphanMediaCleanupManager(
            objectStorageMock.Object,
            productDalMock.Object,
            categoryDalMock.Object,
            sellerProfileDalMock.Object,
            configuration,
            loggerMock.Object);

        await manager.ExecuteAsync();

        objectStorageMock.Verify(service => service.DeleteAsync(orphanKey, It.IsAny<CancellationToken>()), Times.Once);
        objectStorageMock.Verify(service => service.DeleteAsync(keeperKey, It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_WhenStorageIsNotConfigured_ShouldSkipWithoutThrowing()
    {
        var objectStorageMock = new Mock<IObjectStorageService>();
        objectStorageMock
            .Setup(service => service.ListObjectsAsync(null, It.IsAny<int?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("CloudflareR2:AccountId ayarı zorunludur"));

        var productDalMock = new Mock<IProductDal>();
        var categoryDalMock = new Mock<ICategoryDal>();
        var sellerProfileDalMock = new Mock<ISellerProfileDal>();
        var configuration = BuildConfiguration();
        var loggerMock = new Mock<Microsoft.Extensions.Logging.ILogger<OrphanMediaCleanupManager>>();

        var manager = new OrphanMediaCleanupManager(
            objectStorageMock.Object,
            productDalMock.Object,
            categoryDalMock.Object,
            sellerProfileDalMock.Object,
            configuration,
            loggerMock.Object);

        var action = async () => await manager.ExecuteAsync();
        await action.Should().NotThrowAsync();

        objectStorageMock.Verify(service => service.DeleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private static IConfiguration BuildConfiguration(Dictionary<string, string?>? values = null)
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(values ?? new Dictionary<string, string?>())
            .Build();
    }
}
