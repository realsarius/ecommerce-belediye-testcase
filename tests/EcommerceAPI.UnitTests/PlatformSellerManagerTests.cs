using System.Linq.Expressions;
using EcommerceAPI.Business.Concrete;
using EcommerceAPI.Core.Interfaces;
using EcommerceAPI.DataAccess.Abstract;
using EcommerceAPI.Entities.Concrete;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Moq;

namespace EcommerceAPI.UnitTests;

public class PlatformSellerManagerTests
{
    [Fact]
    public async Task GetOrCreatePlatformSellerIdAsync_WhenProfileAlreadyExists_ShouldReturnExistingId()
    {
        var roleDalMock = new Mock<IRoleDal>();
        roleDalMock
            .Setup(x => x.GetAsync(It.IsAny<Expression<Func<Role, bool>>>()))
            .ReturnsAsync(new Role { Id = 3, Name = "Seller", Description = "Seller role" });

        var userDalMock = new Mock<IUserDal>();
        userDalMock
            .Setup(x => x.GetAsync(It.IsAny<Expression<Func<User, bool>>>()))
            .ReturnsAsync(new User
            {
                Id = 21,
                Email = "platform@example.com",
                EmailHash = "hash",
                PasswordHash = "pwd",
                FirstName = "Platform",
                LastName = "Seller",
                RoleId = 3
            });

        var sellerProfileDalMock = new Mock<ISellerProfileDal>();
        sellerProfileDalMock
            .Setup(x => x.GetAsync(It.IsAny<Expression<Func<SellerProfile, bool>>>()))
            .ReturnsAsync(new SellerProfile
            {
                Id = 55,
                UserId = 21,
                BrandName = "Platform Store",
                IsVerified = true
            });

        var hashingMock = new Mock<IHashingService>();
        hashingMock.Setup(x => x.Hash(It.IsAny<string>())).Returns("hashed-email");

        var unitOfWorkMock = new Mock<IUnitOfWork>();
        var loggerMock = new Mock<Microsoft.Extensions.Logging.ILogger<PlatformSellerManager>>();
        var configuration = new ConfigurationBuilder().Build();

        var manager = new PlatformSellerManager(
            userDalMock.Object,
            sellerProfileDalMock.Object,
            roleDalMock.Object,
            hashingMock.Object,
            unitOfWorkMock.Object,
            configuration,
            loggerMock.Object);

        var result = await manager.GetOrCreatePlatformSellerIdAsync();

        result.Success.Should().BeTrue();
        result.Data.Should().Be(55);
        unitOfWorkMock.Verify(x => x.SaveChangesAsync(), Times.Never);
        userDalMock.Verify(x => x.AddAsync(It.IsAny<User>()), Times.Never);
        sellerProfileDalMock.Verify(x => x.AddAsync(It.IsAny<SellerProfile>()), Times.Never);
    }

    [Fact]
    public async Task GetOrCreatePlatformSellerIdAsync_WhenUserAndProfileDoNotExist_ShouldCreateBoth()
    {
        var roleDalMock = new Mock<IRoleDal>();
        roleDalMock
            .Setup(x => x.GetAsync(It.IsAny<Expression<Func<Role, bool>>>()))
            .ReturnsAsync(new Role { Id = 3, Name = "Seller", Description = "Seller role" });

        var userDalMock = new Mock<IUserDal>();
        userDalMock
            .Setup(x => x.GetAsync(It.IsAny<Expression<Func<User, bool>>>()))
            .ReturnsAsync((User?)null);

        User? createdUser = null;
        userDalMock
            .Setup(x => x.AddAsync(It.IsAny<User>()))
            .Callback<User>(entity =>
            {
                createdUser = entity;
                createdUser.Id = 34;
            })
            .ReturnsAsync((User entity) => entity);

        var sellerProfileDalMock = new Mock<ISellerProfileDal>();
        sellerProfileDalMock
            .Setup(x => x.GetAsync(It.IsAny<Expression<Func<SellerProfile, bool>>>()))
            .ReturnsAsync((SellerProfile?)null);

        SellerProfile? createdProfile = null;
        sellerProfileDalMock
            .Setup(x => x.AddAsync(It.IsAny<SellerProfile>()))
            .Callback<SellerProfile>(entity =>
            {
                createdProfile = entity;
                createdProfile.Id = 99;
            })
            .ReturnsAsync((SellerProfile entity) => entity);

        var hashingMock = new Mock<IHashingService>();
        hashingMock.Setup(x => x.Hash("platform-owner@test.local")).Returns("platform-owner-hash");

        var unitOfWorkMock = new Mock<IUnitOfWork>();
        unitOfWorkMock.Setup(x => x.SaveChangesAsync()).ReturnsAsync(1);

        var loggerMock = new Mock<Microsoft.Extensions.Logging.ILogger<PlatformSellerManager>>();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["PlatformSeller:Email"] = "platform-owner@test.local",
                ["PlatformSeller:FirstName"] = "Platform",
                ["PlatformSeller:LastName"] = "Owner",
                ["PlatformSeller:BrandName"] = "Platform Market",
                ["PlatformSeller:BrandDescription"] = "Platform urunleri"
            })
            .Build();

        var manager = new PlatformSellerManager(
            userDalMock.Object,
            sellerProfileDalMock.Object,
            roleDalMock.Object,
            hashingMock.Object,
            unitOfWorkMock.Object,
            configuration,
            loggerMock.Object);

        var result = await manager.GetOrCreatePlatformSellerIdAsync();

        result.Success.Should().BeTrue();
        result.Data.Should().Be(99);

        createdUser.Should().NotBeNull();
        createdUser!.Email.Should().Be("platform-owner@test.local");
        createdUser.FirstName.Should().Be("Platform");
        createdUser.LastName.Should().Be("Owner");
        createdUser.RoleId.Should().Be(3);
        createdUser.IsEmailVerified.Should().BeTrue();

        createdProfile.Should().NotBeNull();
        createdProfile!.UserId.Should().Be(34);
        createdProfile.BrandName.Should().Be("Platform Market");
        createdProfile.BrandDescription.Should().Be("Platform urunleri");
        createdProfile.ContactEmail.Should().Be("platform-owner@test.local");
        createdProfile.IsVerified.Should().BeTrue();

        unitOfWorkMock.Verify(x => x.SaveChangesAsync(), Times.Exactly(2));
        userDalMock.Verify(x => x.AddAsync(It.IsAny<User>()), Times.Once);
        sellerProfileDalMock.Verify(x => x.AddAsync(It.IsAny<SellerProfile>()), Times.Once);
    }
}
