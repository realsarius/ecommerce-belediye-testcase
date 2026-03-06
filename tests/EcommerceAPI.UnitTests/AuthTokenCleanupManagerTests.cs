using EcommerceAPI.Business.Concrete;
using EcommerceAPI.Core.Interfaces;
using EcommerceAPI.Application.Abstractions.Persistence;
using EcommerceAPI.Entities.Concrete;
using FluentAssertions;
using Moq;

namespace EcommerceAPI.UnitTests;

public class AuthTokenCleanupManagerTests
{
    [Fact]
    public async Task ExecuteAsync_WhenExpiredTokensExist_ShouldClearAndPersistChanges()
    {
        var now = DateTime.UtcNow;
        var user = new User
        {
            Id = 101,
            Email = "user@example.com",
            EmailHash = "hash",
            PasswordHash = "password-hash",
            FirstName = "Berkan",
            LastName = "Sözer",
            RoleId = 1,
            EmailVerificationToken = "expired-verify-token",
            EmailVerificationTokenExpiry = now.AddMinutes(-1),
            PasswordResetToken = "expired-reset-token",
            PasswordResetTokenExpiry = now.AddMinutes(-1),
            PendingEmail = "new@example.com",
            EmailChangeToken = "expired-change-token",
            EmailChangeTokenExpiry = now.AddMinutes(-1)
        };

        var userDalMock = new Mock<IUserDal>();
        userDalMock
            .Setup(x => x.GetListAsync(It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>()))
            .ReturnsAsync([user]);

        var unitOfWorkMock = new Mock<IUnitOfWork>();
        var loggerMock = new Mock<Microsoft.Extensions.Logging.ILogger<AuthTokenCleanupManager>>();

        var manager = new AuthTokenCleanupManager(
            userDalMock.Object,
            unitOfWorkMock.Object,
            loggerMock.Object);

        await manager.ExecuteAsync();

        user.EmailVerificationToken.Should().BeNull();
        user.EmailVerificationTokenExpiry.Should().BeNull();
        user.PasswordResetToken.Should().BeNull();
        user.PasswordResetTokenExpiry.Should().BeNull();
        user.PendingEmail.Should().BeNull();
        user.EmailChangeToken.Should().BeNull();
        user.EmailChangeTokenExpiry.Should().BeNull();

        userDalMock.Verify(x => x.Update(user), Times.Once);
        unitOfWorkMock.Verify(x => x.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_WhenNoExpiredTokenExists_ShouldSkipPersistence()
    {
        var now = DateTime.UtcNow;
        var user = new User
        {
            Id = 202,
            Email = "active@example.com",
            EmailHash = "hash",
            PasswordHash = "password-hash",
            FirstName = "Test",
            LastName = "User",
            RoleId = 1,
            EmailVerificationToken = "active-token",
            EmailVerificationTokenExpiry = now.AddHours(1)
        };

        var userDalMock = new Mock<IUserDal>();
        userDalMock
            .Setup(x => x.GetListAsync(It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>()))
            .ReturnsAsync([user]);

        var unitOfWorkMock = new Mock<IUnitOfWork>();
        var loggerMock = new Mock<Microsoft.Extensions.Logging.ILogger<AuthTokenCleanupManager>>();

        var manager = new AuthTokenCleanupManager(
            userDalMock.Object,
            unitOfWorkMock.Object,
            loggerMock.Object);

        await manager.ExecuteAsync();

        user.EmailVerificationToken.Should().Be("active-token");
        user.EmailVerificationTokenExpiry.Should().NotBeNull();

        userDalMock.Verify(x => x.Update(It.IsAny<User>()), Times.Never);
        unitOfWorkMock.Verify(x => x.SaveChangesAsync(), Times.Never);
    }
}

