using EcommerceAPI.Business.Abstract;
using EcommerceAPI.Business.Concrete;
using EcommerceAPI.Business.Constants;
using EcommerceAPI.Core.CrossCuttingConcerns.Logging;
using EcommerceAPI.Core.Interfaces;
using EcommerceAPI.Core.Utilities.Results;
using EcommerceAPI.DataAccess.Abstract;
using EcommerceAPI.Entities.Concrete;
using EcommerceAPI.Entities.DTOs;
using EcommerceAPI.Entities.Enums;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Moq;

namespace EcommerceAPI.UnitTests;

public class AuthManagerTests
{
    private readonly Mock<IUserDal> _userDalMock;
    private readonly Mock<IRoleDal> _roleDalMock;
    private readonly Mock<IRefreshTokenDal> _refreshTokenDalMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IHashingService> _hashingServiceMock;
    private readonly Mock<IAuditService> _auditServiceMock;
    private readonly Mock<ITokenHelper> _tokenHelperMock;
    private readonly Mock<ISocialAuthValidator> _socialAuthValidatorMock;
    private readonly Mock<IReferralService> _referralServiceMock;
    private readonly AuthManager _manager;

    public AuthManagerTests()
    {
        _userDalMock = new Mock<IUserDal>();
        _roleDalMock = new Mock<IRoleDal>();
        _refreshTokenDalMock = new Mock<IRefreshTokenDal>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _hashingServiceMock = new Mock<IHashingService>();
        _auditServiceMock = new Mock<IAuditService>();
        _tokenHelperMock = new Mock<ITokenHelper>();
        _socialAuthValidatorMock = new Mock<ISocialAuthValidator>();
        _referralServiceMock = new Mock<IReferralService>();
        _referralServiceMock.Setup(x => x.ValidateReferralCodeAsync(It.IsAny<string?>()))
            .ReturnsAsync(new SuccessResult());
        _referralServiceMock.Setup(x => x.SetupNewUserAsync(It.IsAny<int>(), It.IsAny<string?>()))
            .ReturnsAsync(new SuccessResult());

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["JWT_EXPIRATION_MINUTES"] = "60"
            })
            .Build();

        _manager = new AuthManager(
            _userDalMock.Object,
            _roleDalMock.Object,
            _refreshTokenDalMock.Object,
            _unitOfWorkMock.Object,
            configuration,
            _hashingServiceMock.Object,
            _auditServiceMock.Object,
            _tokenHelperMock.Object,
            _socialAuthValidatorMock.Object,
            _referralServiceMock.Object);
    }

    [Fact]
    public async Task SocialLoginAsync_WhenProviderValidationFails_ShouldReturnError()
    {
        _socialAuthValidatorMock
            .Setup(x => x.ValidateAsync("google", "bad-token", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SocialAuthValidationResult
            {
                Success = false,
                ErrorMessage = "Google kimlik doğrulaması başarısız oldu."
            });

        var result = await _manager.SocialLoginAsync(new SocialLoginRequest
        {
            Provider = "google",
            IdToken = "bad-token"
        });

        result.Success.Should().BeFalse();
        result.Data.Should().NotBeNull();
        result.Data!.Message.Should().Be("Google kimlik doğrulaması başarısız oldu.");
        _userDalMock.Verify(x => x.AddAsync(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task SocialLoginAsync_WhenUserDoesNotExist_ShouldCreateCustomerUserAndIssueTokens()
    {
        const string normalizedEmail = "social@example.com";
        const string emailHash = "hash-social@example.com";

        _socialAuthValidatorMock
            .Setup(x => x.ValidateAsync("google", "valid-token", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SocialAuthValidationResult
            {
                Success = true,
                Provider = "google",
                Subject = "google-subject-1",
                Email = normalizedEmail,
                EmailVerified = true,
                FirstName = "Berkan",
                LastName = "Sözer"
            });

        _hashingServiceMock
            .Setup(x => x.Hash(normalizedEmail))
            .Returns(emailHash);
        _userDalMock
            .Setup(x => x.GetListAsync(It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>()))
            .ReturnsAsync([]);
        _roleDalMock
            .Setup(x => x.GetListAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Role, bool>>>()))
            .ReturnsAsync([new Role { Id = 7, Name = "Customer" }]);
        _roleDalMock
            .Setup(x => x.GetAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Role, bool>>>()))
            .ReturnsAsync(new Role { Id = 7, Name = "Customer" });
        _tokenHelperMock.Setup(x => x.GenerateAccessToken(It.IsAny<int>(), normalizedEmail, "Customer", It.IsAny<string>(), It.IsAny<string>()))
            .Returns("access-token");
        _tokenHelperMock.Setup(x => x.GenerateRefreshToken())
            .Returns("refresh-token");
        _hashingServiceMock
            .Setup(x => x.Hash("refresh-token"))
            .Returns("hashed-refresh-token");

        User? addedUser = null;
        _userDalMock
            .Setup(x => x.AddAsync(It.IsAny<User>()))
            .Callback<User>(user =>
            {
                user.Id = 123;
                addedUser = user;
            })
            .ReturnsAsync((User user) => user);

        var result = await _manager.SocialLoginAsync(new SocialLoginRequest
        {
            Provider = "google",
            IdToken = "valid-token"
        });

        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        var authResponse = result.Data!;
        authResponse.Success.Should().BeTrue();
        authResponse.Token.Should().Be("access-token");
        authResponse.RefreshToken.Should().Be("refresh-token");
        authResponse.User.Should().NotBeNull();
        authResponse.User!.Email.Should().Be(normalizedEmail);
        addedUser.Should().NotBeNull();
        var createdUser = addedUser!;
        createdUser.GoogleSubject.Should().Be("google-subject-1");
        createdUser.IsEmailVerified.Should().BeTrue();
        createdUser.PasswordHash.Should().BeEmpty();
        _refreshTokenDalMock.Verify(x => x.AddAsync(It.IsAny<RefreshToken>()), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(), Times.Exactly(3));
    }

    [Fact]
    public async Task LoginAsync_WhenAccountIsSocialOnly_ShouldReturnFriendlyError()
    {
        _hashingServiceMock
            .Setup(x => x.Hash("social@example.com"))
            .Returns("hash-social");
        _userDalMock
            .Setup(x => x.GetListAsync(It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>()))
            .ReturnsAsync([
                new User
                {
                    Id = 9,
                    Email = "social@example.com",
                    EmailHash = "hash-social",
                    PasswordHash = string.Empty,
                    GoogleSubject = "google-subject-1",
                    RoleId = 1
                }
            ]);

        var result = await _manager.LoginAsync(new LoginRequest
        {
            Email = "social@example.com",
            Password = "anything"
        });

        result.Success.Should().BeFalse();
        result.Data.Should().NotBeNull();
        result.Data!.Message.Should().Be(Messages.SocialAccountPasswordLoginNotAllowed);
    }

    [Fact]
    public async Task LoginAsync_WhenAccountIsSuspended_ShouldReturnStatusError()
    {
        _hashingServiceMock
            .Setup(x => x.Hash("suspended@example.com"))
            .Returns("hash-suspended");
        _userDalMock
            .Setup(x => x.GetListAsync(It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>()))
            .ReturnsAsync([
                new User
                {
                    Id = 19,
                    Email = "suspended@example.com",
                    EmailHash = "hash-suspended",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("Test123!"),
                    RoleId = 2,
                    AccountStatus = UserAccountStatus.Suspended
                }
            ]);

        var result = await _manager.LoginAsync(new LoginRequest
        {
            Email = "suspended@example.com",
            Password = "Test123!"
        });

        result.Success.Should().BeFalse();
        result.Data.Should().NotBeNull();
        result.Data!.Message.Should().Be("Hesabınız geçici olarak askıya alınmıştır.");
    }

    [Fact]
    public async Task RefreshTokenAsync_WhenAccountIsBanned_ShouldReturnStatusError()
    {
        _hashingServiceMock
            .Setup(x => x.Hash("refresh-token"))
            .Returns("hashed-refresh-token");

        _refreshTokenDalMock
            .Setup(x => x.GetListAsync(It.IsAny<System.Linq.Expressions.Expression<Func<RefreshToken, bool>>>()))
            .ReturnsAsync([
                new RefreshToken
                {
                    Id = 1,
                    UserId = 29,
                    Token = "hashed-refresh-token",
                    ExpiresAt = DateTime.UtcNow.AddDays(3),
                    IsRevoked = false,
                    IsUsed = false
                }
            ]);

        _userDalMock
            .Setup(x => x.GetAsync(It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>()))
            .ReturnsAsync(new User
            {
                Id = 29,
                Email = "banned@example.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Test123!"),
                FirstName = "Banned",
                LastName = "User",
                RoleId = 2,
                AccountStatus = UserAccountStatus.Banned
            });

        var result = await _manager.RefreshTokenAsync(new RefreshTokenRequest
        {
            RefreshToken = "refresh-token"
        });

        result.Success.Should().BeFalse();
        result.Data.Should().NotBeNull();
        result.Data!.Message.Should().Be("Hesabınız kullanım dışı bırakılmıştır.");
    }
}
