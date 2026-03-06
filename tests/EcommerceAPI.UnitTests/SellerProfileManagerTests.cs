using EcommerceAPI.Business.Concrete;
using EcommerceAPI.Core.Interfaces;
using EcommerceAPI.DataAccess.Abstract;
using EcommerceAPI.Entities.Concrete;
using EcommerceAPI.Entities.DTOs;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Moq;

namespace EcommerceAPI.UnitTests;

public class SellerProfileManagerTests
{
    [Fact]
    public async Task UpdateAsync_WhenUserIsPlatformSeller_ShouldReturnAuthorizationError()
    {
        var sellerProfileDalMock = new Mock<ISellerProfileDal>();
        var userDalMock = new Mock<IUserDal>();
        var hashingServiceMock = new Mock<IHashingService>();
        hashingServiceMock.Setup(service => service.Hash("platform-seller@system.local")).Returns("platform-hash");
        userDalMock
            .Setup(dal => dal.GetAsync(It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>()))
            .ReturnsAsync(new User
            {
                Id = 901,
                Email = "platform-seller@system.local",
                EmailHash = "platform-hash",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Strong123!")
            });

        var unitOfWorkMock = new Mock<IUnitOfWork>();
        var loggerMock = new Mock<Microsoft.Extensions.Logging.ILogger<SellerProfileManager>>();
        var configuration = new ConfigurationBuilder().Build();

        var manager = new SellerProfileManager(
            sellerProfileDalMock.Object,
            userDalMock.Object,
            unitOfWorkMock.Object,
            hashingServiceMock.Object,
            configuration,
            loggerMock.Object);

        var result = await manager.UpdateAsync(901, new UpdateSellerProfileRequest
        {
            BrandName = "Blocked Update"
        });

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Yetkiniz yok");
        sellerProfileDalMock.Verify(dal => dal.GetByUserIdWithDetailsAsync(It.IsAny<int>()), Times.Never);
        sellerProfileDalMock.Verify(dal => dal.Update(It.IsAny<SellerProfile>()), Times.Never);
        unitOfWorkMock.Verify(unit => unit.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_WhenUserIsPlatformSeller_ShouldReturnAuthorizationError()
    {
        var sellerProfileDalMock = new Mock<ISellerProfileDal>();
        var userDalMock = new Mock<IUserDal>();
        var hashingServiceMock = new Mock<IHashingService>();
        hashingServiceMock.Setup(service => service.Hash("platform-seller@system.local")).Returns("platform-hash");
        userDalMock
            .Setup(dal => dal.GetAsync(It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>()))
            .ReturnsAsync(new User
            {
                Id = 902,
                Email = "platform-seller@system.local",
                EmailHash = "platform-hash",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Strong123!")
            });

        var unitOfWorkMock = new Mock<IUnitOfWork>();
        var loggerMock = new Mock<Microsoft.Extensions.Logging.ILogger<SellerProfileManager>>();
        var configuration = new ConfigurationBuilder().Build();

        var manager = new SellerProfileManager(
            sellerProfileDalMock.Object,
            userDalMock.Object,
            unitOfWorkMock.Object,
            hashingServiceMock.Object,
            configuration,
            loggerMock.Object);

        var result = await manager.DeleteAsync(902);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Yetkiniz yok");
        sellerProfileDalMock.Verify(dal => dal.GetAsync(It.IsAny<System.Linq.Expressions.Expression<Func<SellerProfile, bool>>>()), Times.Never);
        sellerProfileDalMock.Verify(dal => dal.Delete(It.IsAny<SellerProfile>()), Times.Never);
        unitOfWorkMock.Verify(unit => unit.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_WhenUserIsRegularSeller_ShouldUpdateProfile()
    {
        var profile = new SellerProfile
        {
            Id = 77,
            UserId = 903,
            BrandName = "Old Brand",
            ContactEmail = "old@seller.test",
            User = new User
            {
                Id = 903,
                Email = "seller903@test.local",
                FirstName = "Seller",
                LastName = "User",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Strong123!")
            }
        };

        var sellerProfileDalMock = new Mock<ISellerProfileDal>();
        sellerProfileDalMock
            .Setup(dal => dal.GetByUserIdWithDetailsAsync(903))
            .ReturnsAsync(profile);

        var userDalMock = new Mock<IUserDal>();
        var hashingServiceMock = new Mock<IHashingService>();
        hashingServiceMock.Setup(service => service.Hash("platform-seller@system.local")).Returns("platform-hash");
        userDalMock
            .Setup(dal => dal.GetAsync(It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>()))
            .ReturnsAsync(new User
            {
                Id = 903,
                Email = "seller903@test.local",
                EmailHash = "seller-hash",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Strong123!")
            });

        var unitOfWorkMock = new Mock<IUnitOfWork>();
        unitOfWorkMock.Setup(unit => unit.SaveChangesAsync()).ReturnsAsync(1);

        var loggerMock = new Mock<Microsoft.Extensions.Logging.ILogger<SellerProfileManager>>();
        var configuration = new ConfigurationBuilder().Build();

        var manager = new SellerProfileManager(
            sellerProfileDalMock.Object,
            userDalMock.Object,
            unitOfWorkMock.Object,
            hashingServiceMock.Object,
            configuration,
            loggerMock.Object);

        var result = await manager.UpdateAsync(903, new UpdateSellerProfileRequest
        {
            BrandName = "New Brand",
            ContactEmail = "new@seller.test"
        });

        result.Success.Should().BeTrue();
        profile.BrandName.Should().Be("New Brand");
        profile.ContactEmail.Should().Be("new@seller.test");
        sellerProfileDalMock.Verify(dal => dal.Update(It.Is<SellerProfile>(entity => entity.Id == 77)), Times.Once);
        unitOfWorkMock.Verify(unit => unit.SaveChangesAsync(), Times.Once);
    }
}
