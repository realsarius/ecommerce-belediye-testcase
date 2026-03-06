using EcommerceAPI.Application.Abstractions.ServiceContracts;
using EcommerceAPI.Business.Concrete;
using EcommerceAPI.Core.Interfaces;
using EcommerceAPI.Core.Utilities.Results;
using EcommerceAPI.Application.Abstractions.Persistence;
using FluentAssertions;
using Moq;

namespace EcommerceAPI.UnitTests;

public class PlatformProductBackfillManagerTests
{
    [Fact]
    public async Task GetProductIdsWithoutSellerSnapshotAsync_ShouldReturnIdsFromRepository()
    {
        var expectedIds = new List<int> { 3, 8, 21 };

        var productDalMock = new Mock<IProductDal>();
        productDalMock
            .Setup(dal => dal.GetProductIdsWithoutSellerAsync())
            .ReturnsAsync(expectedIds);

        var platformSellerServiceMock = new Mock<IPlatformSellerService>();
        var unitOfWorkMock = new Mock<IUnitOfWork>();
        var loggerMock = new Mock<Microsoft.Extensions.Logging.ILogger<PlatformProductBackfillManager>>();

        var manager = new PlatformProductBackfillManager(
            productDalMock.Object,
            platformSellerServiceMock.Object,
            unitOfWorkMock.Object,
            loggerMock.Object);

        var snapshotIds = await manager.GetProductIdsWithoutSellerSnapshotAsync();

        snapshotIds.Should().Equal(expectedIds);
        productDalMock.Verify(dal => dal.GetProductIdsWithoutSellerAsync(), Times.Once);
    }

    [Fact]
    public async Task BackfillMissingSellerIdsAsync_WhenNoMissingProductExists_ShouldSkipBackfill()
    {
        var productDalMock = new Mock<IProductDal>();
        productDalMock
            .Setup(dal => dal.CountProductsWithoutSellerAsync())
            .ReturnsAsync(0);

        var platformSellerServiceMock = new Mock<IPlatformSellerService>();
        var unitOfWorkMock = new Mock<IUnitOfWork>();
        var loggerMock = new Mock<Microsoft.Extensions.Logging.ILogger<PlatformProductBackfillManager>>();

        var manager = new PlatformProductBackfillManager(
            productDalMock.Object,
            platformSellerServiceMock.Object,
            unitOfWorkMock.Object,
            loggerMock.Object);

        var result = await manager.BackfillMissingSellerIdsAsync();

        result.Success.Should().BeTrue();
        platformSellerServiceMock.Verify(service => service.GetOrCreatePlatformSellerIdAsync(), Times.Never);
        productDalMock.Verify(dal => dal.BackfillMissingSellerIdsAsync(It.IsAny<int>(), It.IsAny<DateTime>()), Times.Never);
        unitOfWorkMock.Verify(unit => unit.BeginTransactionAsync(), Times.Never);
        unitOfWorkMock.Verify(unit => unit.CommitTransactionAsync(), Times.Never);
    }

    [Fact]
    public async Task BackfillMissingSellerIdsAsync_WhenMissingProductsExist_ShouldBackfillWithinTransaction()
    {
        var productDalMock = new Mock<IProductDal>();
        var countCalls = 0;
        productDalMock
            .Setup(dal => dal.CountProductsWithoutSellerAsync())
            .ReturnsAsync(() =>
            {
                countCalls++;
                return countCalls == 1 ? 3 : 0;
            });
        productDalMock
            .Setup(dal => dal.BackfillMissingSellerIdsAsync(42, It.IsAny<DateTime>()))
            .ReturnsAsync(3);

        var platformSellerServiceMock = new Mock<IPlatformSellerService>();
        platformSellerServiceMock
            .Setup(service => service.GetOrCreatePlatformSellerIdAsync())
            .ReturnsAsync(new SuccessDataResult<int>(42));

        var unitOfWorkMock = new Mock<IUnitOfWork>();
        unitOfWorkMock.Setup(unit => unit.BeginTransactionAsync()).Returns(Task.CompletedTask);
        unitOfWorkMock.Setup(unit => unit.CommitTransactionAsync()).Returns(Task.CompletedTask);
        var loggerMock = new Mock<Microsoft.Extensions.Logging.ILogger<PlatformProductBackfillManager>>();

        var manager = new PlatformProductBackfillManager(
            productDalMock.Object,
            platformSellerServiceMock.Object,
            unitOfWorkMock.Object,
            loggerMock.Object);

        var result = await manager.BackfillMissingSellerIdsAsync();

        result.Success.Should().BeTrue();
        productDalMock.Verify(dal => dal.BackfillMissingSellerIdsAsync(42, It.IsAny<DateTime>()), Times.Once);
        unitOfWorkMock.Verify(unit => unit.BeginTransactionAsync(), Times.Once);
        unitOfWorkMock.Verify(unit => unit.CommitTransactionAsync(), Times.Once);
        unitOfWorkMock.Verify(unit => unit.RollbackTransactionAsync(), Times.Never);
    }

    [Fact]
    public async Task BackfillMissingSellerIdsAsync_WhenBackfillThrows_ShouldRollbackTransaction()
    {
        var productDalMock = new Mock<IProductDal>();
        productDalMock
            .SetupSequence(dal => dal.CountProductsWithoutSellerAsync())
            .ReturnsAsync(2);
        productDalMock
            .Setup(dal => dal.BackfillMissingSellerIdsAsync(42, It.IsAny<DateTime>()))
            .ThrowsAsync(new InvalidOperationException("boom"));

        var platformSellerServiceMock = new Mock<IPlatformSellerService>();
        platformSellerServiceMock
            .Setup(service => service.GetOrCreatePlatformSellerIdAsync())
            .ReturnsAsync(new SuccessDataResult<int>(42));

        var unitOfWorkMock = new Mock<IUnitOfWork>();
        unitOfWorkMock.Setup(unit => unit.BeginTransactionAsync()).Returns(Task.CompletedTask);
        unitOfWorkMock.Setup(unit => unit.RollbackTransactionAsync()).Returns(Task.CompletedTask);
        var loggerMock = new Mock<Microsoft.Extensions.Logging.ILogger<PlatformProductBackfillManager>>();

        var manager = new PlatformProductBackfillManager(
            productDalMock.Object,
            platformSellerServiceMock.Object,
            unitOfWorkMock.Object,
            loggerMock.Object);

        var result = await manager.BackfillMissingSellerIdsAsync();

        result.Success.Should().BeFalse();
        unitOfWorkMock.Verify(unit => unit.BeginTransactionAsync(), Times.Once);
        unitOfWorkMock.Verify(unit => unit.RollbackTransactionAsync(), Times.Once);
        unitOfWorkMock.Verify(unit => unit.CommitTransactionAsync(), Times.Never);
    }
}
