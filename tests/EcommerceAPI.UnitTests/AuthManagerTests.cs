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
    private readonly Mock<IEmailNotificationService> _emailNotificationServiceMock;
    private readonly Mock<IAuthRateLimitService> _authRateLimitServiceMock;
    private readonly Mock<Microsoft.Extensions.Logging.ILogger<AuthManager>> _loggerMock;
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
        _emailNotificationServiceMock = new Mock<IEmailNotificationService>();
        _authRateLimitServiceMock = new Mock<IAuthRateLimitService>();
        _loggerMock = new Mock<Microsoft.Extensions.Logging.ILogger<AuthManager>>();
        _referralServiceMock.Setup(x => x.ValidateReferralCodeAsync(It.IsAny<string?>()))
            .ReturnsAsync(new SuccessResult());
        _referralServiceMock.Setup(x => x.SetupNewUserAsync(It.IsAny<int>(), It.IsAny<string?>()))
            .ReturnsAsync(new SuccessResult());
        _emailNotificationServiceMock.Setup(x => x.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _authRateLimitServiceMock.Setup(x => x.TryConsumeForgotPasswordAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, 0));
        _authRateLimitServiceMock.Setup(x => x.TryConsumeResendVerificationAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, 0));

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
            _referralServiceMock.Object,
            _emailNotificationServiceMock.Object,
            _authRateLimitServiceMock.Object,
            _loggerMock.Object);
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
        _tokenHelperMock.Setup(x => x.GenerateAccessToken(It.IsAny<int>(), normalizedEmail, "Customer", It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()))
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

    [Fact]
    public async Task ForgotPasswordAsync_WhenEmailDoesNotExist_ShouldReturnSuccessWithoutSendingEmail()
    {
        _hashingServiceMock
            .Setup(x => x.Hash("missing@example.com"))
            .Returns("missing-hash");
        _userDalMock
            .Setup(x => x.GetListAsync(It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>()))
            .ReturnsAsync([]);

        var result = await _manager.ForgotPasswordAsync(new ForgotPasswordRequest
        {
            Email = "missing@example.com"
        });

        result.Success.Should().BeTrue();
        result.Message.Should().Be(Messages.PasswordResetLinkSent);
        _emailNotificationServiceMock.Verify(
            x => x.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ForgotPasswordAsync_WhenRateLimitExceeded_ShouldReturnRateLimitError()
    {
        _hashingServiceMock
            .Setup(x => x.Hash("limited@example.com"))
            .Returns("limited-hash");
        _authRateLimitServiceMock
            .Setup(x => x.TryConsumeForgotPasswordAsync("limited-hash", It.IsAny<CancellationToken>()))
            .ReturnsAsync((false, 1800));

        var result = await _manager.ForgotPasswordAsync(new ForgotPasswordRequest
        {
            Email = "limited@example.com"
        });

        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be(ErrorCodes.RateLimitExceeded);
        _userDalMock.Verify(
            x => x.GetListAsync(It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>()),
            Times.Never);
    }

    [Fact]
    public async Task VerifyEmailAsync_WhenTokenExpired_ShouldClearStoredTokenAndReturnExpiredError()
    {
        var user = new User
        {
            Id = 71,
            Email = "verify@example.com",
            EmailHash = "verify-hash",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Strong123!"),
            FirstName = "Verify",
            LastName = "User",
            RoleId = 1,
            EmailVerificationToken = "verify-token-hash",
            EmailVerificationTokenExpiry = DateTime.UtcNow.AddMinutes(-5),
            IsEmailVerified = false
        };

        _hashingServiceMock
            .Setup(x => x.Hash("expired-verify-token"))
            .Returns("verify-token-hash");
        _userDalMock
            .Setup(x => x.GetListAsync(It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>()))
            .ReturnsAsync([user]);

        var result = await _manager.VerifyEmailAsync(new VerifyEmailRequest
        {
            Token = "expired-verify-token"
        });

        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be(ErrorCodes.ExpiredToken);
        user.EmailVerificationToken.Should().BeNull();
        user.EmailVerificationTokenExpiry.Should().BeNull();
        _userDalMock.Verify(x => x.Update(It.Is<User>(u => u.Id == 71)), Times.Once);
    }

    [Fact]
    public async Task ResendVerificationAsync_WhenRateLimitExceeded_ShouldReturnRateLimitError()
    {
        var user = new User
        {
            Id = 81,
            Email = "resend@example.com",
            EmailHash = "resend-hash",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Strong123!"),
            FirstName = "Resend",
            LastName = "User",
            RoleId = 1,
            IsEmailVerified = false
        };

        _userDalMock
            .Setup(x => x.GetAsync(It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>()))
            .ReturnsAsync(user);
        _authRateLimitServiceMock
            .Setup(x => x.TryConsumeResendVerificationAsync(81, It.IsAny<CancellationToken>()))
            .ReturnsAsync((false, 120));

        var result = await _manager.ResendVerificationAsync(81);

        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be(ErrorCodes.RateLimitExceeded);
        _userDalMock.Verify(x => x.Update(It.IsAny<User>()), Times.Never);
        _emailNotificationServiceMock.Verify(
            x => x.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ResetPasswordAsync_WhenPasswordsMismatch_ShouldReturnPasswordMismatchError()
    {
        var user = new User
        {
            Id = 91,
            Email = "reset@example.com",
            EmailHash = "reset-hash",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("OldPassword1"),
            FirstName = "Reset",
            LastName = "User",
            RoleId = 1,
            PasswordResetToken = "reset-token-hash",
            PasswordResetTokenExpiry = DateTime.UtcNow.AddMinutes(30)
        };

        _hashingServiceMock
            .Setup(x => x.Hash("valid-reset-token"))
            .Returns("reset-token-hash");
        _userDalMock
            .Setup(x => x.GetListAsync(It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>()))
            .ReturnsAsync([user]);

        var result = await _manager.ResetPasswordAsync(new ResetPasswordRequest
        {
            Token = "valid-reset-token",
            NewPassword = "NewPassword1",
            ConfirmPassword = "DifferentPassword1"
        });

        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be(ErrorCodes.PasswordMismatch);
        user.PasswordResetToken.Should().Be("reset-token-hash");
        _refreshTokenDalMock.Verify(
            x => x.GetListAsync(It.IsAny<System.Linq.Expressions.Expression<Func<RefreshToken, bool>>>()),
            Times.Never);
    }

    [Fact]
    public async Task ResetPasswordAsync_WhenTokenIsValid_ShouldRevokeAllRefreshTokens()
    {
        var user = new User
        {
            Id = 101,
            Email = "reset-valid@example.com",
            EmailHash = "reset-valid-hash",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("OldPassword1"),
            FirstName = "Reset",
            LastName = "Valid",
            RoleId = 1,
            PasswordResetToken = "reset-token-valid-hash",
            PasswordResetTokenExpiry = DateTime.UtcNow.AddMinutes(30)
        };

        var refreshTokens = new List<RefreshToken>
        {
            new() { Id = 1, UserId = 101, Token = "rt-1", ExpiresAt = DateTime.UtcNow.AddDays(1), IsRevoked = false, IsUsed = false },
            new() { Id = 2, UserId = 101, Token = "rt-2", ExpiresAt = DateTime.UtcNow.AddDays(1), IsRevoked = false, IsUsed = false }
        };

        _hashingServiceMock
            .Setup(x => x.Hash("valid-reset-token"))
            .Returns("reset-token-valid-hash");
        _userDalMock
            .Setup(x => x.GetListAsync(It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>()))
            .ReturnsAsync([user]);
        _refreshTokenDalMock
            .Setup(x => x.GetListAsync(It.IsAny<System.Linq.Expressions.Expression<Func<RefreshToken, bool>>>()))
            .ReturnsAsync(refreshTokens);

        var result = await _manager.ResetPasswordAsync(new ResetPasswordRequest
        {
            Token = "valid-reset-token",
            NewPassword = "NewPassword1",
            ConfirmPassword = "NewPassword1"
        });

        result.Success.Should().BeTrue();
        result.Message.Should().Be(Messages.PasswordResetSuccess);
        BCrypt.Net.BCrypt.Verify("NewPassword1", user.PasswordHash).Should().BeTrue();
        user.PasswordResetToken.Should().BeNull();
        user.PasswordResetTokenExpiry.Should().BeNull();
        refreshTokens.Should().OnlyContain(token =>
            token.IsRevoked &&
            token.RevokedReason == "Password reset");
        _refreshTokenDalMock.Verify(
            x => x.Update(It.Is<RefreshToken>(token => token.UserId == 101 && token.IsRevoked)),
            Times.Exactly(2));
    }

    [Fact]
    public async Task ChangeEmailAsync_WhenCurrentPasswordIsInvalid_ShouldReturnError()
    {
        var existingUser = new User
        {
            Id = 41,
            Email = "old@example.com",
            EmailHash = "old-hash",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Correct123!"),
            FirstName = "Berkan",
            LastName = "Sözer",
            RoleId = 1,
            AccountStatus = UserAccountStatus.Active,
            IsEmailVerified = true
        };

        _userDalMock
            .Setup(x => x.GetAsync(It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>()))
            .ReturnsAsync(existingUser);

        var result = await _manager.ChangeEmailAsync(41, new ChangeEmailRequest
        {
            NewEmail = "new@example.com",
            CurrentPassword = "WrongPassword1!"
        });

        result.Success.Should().BeFalse();
        result.Message.Should().Be(Messages.CurrentPasswordInvalid);
        _userDalMock.Verify(x => x.Update(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task ChangeEmailAsync_WhenRequestIsValid_ShouldPersistPendingEmailAndSendVerificationMail()
    {
        var existingUser = new User
        {
            Id = 51,
            Email = "old@example.com",
            EmailHash = "old-hash",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Correct123!"),
            FirstName = "Berkan",
            LastName = "Sözer",
            RoleId = 1,
            AccountStatus = UserAccountStatus.Active,
            IsEmailVerified = true
        };

        _userDalMock
            .Setup(x => x.GetAsync(It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>()))
            .ReturnsAsync(existingUser);
        _userDalMock
            .Setup(x => x.GetListAsync(It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>()))
            .ReturnsAsync([]);
        _hashingServiceMock
            .Setup(x => x.Hash(It.IsAny<string>()))
            .Returns<string>(input =>
            {
                if (string.Equals(input, "new@example.com", StringComparison.OrdinalIgnoreCase))
                {
                    return "new-email-hash";
                }

                return "generated-token-hash";
            });

        User? updatedUser = null;
        _userDalMock
            .Setup(x => x.Update(It.IsAny<User>()))
            .Callback<User>(user => updatedUser = user);

        var result = await _manager.ChangeEmailAsync(51, new ChangeEmailRequest
        {
            NewEmail = "new@example.com",
            CurrentPassword = "Correct123!"
        });

        result.Success.Should().BeTrue();
        result.Message.Should().Be(Messages.EmailChangeVerificationSent);
        updatedUser.Should().NotBeNull();
        updatedUser!.PendingEmail.Should().Be("new@example.com");
        updatedUser.EmailChangeToken.Should().Be("generated-token-hash");
        updatedUser.EmailChangeTokenExpiry.Should().NotBeNull();
        _emailNotificationServiceMock.Verify(
            x => x.SendAsync(
                "new@example.com",
                It.Is<string>(subject => subject.Contains("Yeni e-posta", StringComparison.OrdinalIgnoreCase)),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ConfirmEmailChangeAsync_WhenTokenIsValid_ShouldUpdateEmailAndIssueTokens()
    {
        var user = new User
        {
            Id = 61,
            Email = "old@example.com",
            EmailHash = "old-email-hash",
            PendingEmail = "newmail@example.com",
            EmailChangeToken = "confirm-token-hash",
            EmailChangeTokenExpiry = DateTime.UtcNow.AddHours(12),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Correct123!"),
            FirstName = "Berkan",
            LastName = "Sözer",
            RoleId = 7,
            AccountStatus = UserAccountStatus.Active,
            IsEmailVerified = true
        };

        _hashingServiceMock
            .Setup(x => x.Hash(It.IsAny<string>()))
            .Returns<string>(value => value switch
            {
                "confirm-token" => "confirm-token-hash",
                "newmail@example.com" => "new-email-hash",
                "refresh-token-new" => "refresh-token-new-hash",
                _ => "generic-hash"
            });

        _userDalMock
            .SetupSequence(x => x.GetListAsync(It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>()))
            .ReturnsAsync([user])
            .ReturnsAsync([]);

        _refreshTokenDalMock
            .Setup(x => x.GetListAsync(It.IsAny<System.Linq.Expressions.Expression<Func<RefreshToken, bool>>>()))
            .ReturnsAsync([
                new RefreshToken
                {
                    Id = 1,
                    UserId = 61,
                    Token = "existing-refresh-token",
                    IsRevoked = false,
                    ExpiresAt = DateTime.UtcNow.AddDays(3)
                }
            ]);

        _roleDalMock
            .Setup(x => x.GetAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Role, bool>>>()))
            .ReturnsAsync(new Role { Id = 7, Name = "Customer" });

        _tokenHelperMock
            .Setup(x => x.GenerateAccessToken(61, "newmail@example.com", "Customer", "Berkan", "Sözer", true))
            .Returns("new-access-token");
        _tokenHelperMock
            .Setup(x => x.GenerateRefreshToken())
            .Returns("refresh-token-new");

        var result = await _manager.ConfirmEmailChangeAsync(new ConfirmEmailChangeRequest
        {
            Token = "confirm-token"
        });

        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Token.Should().Be("new-access-token");
        result.Data.RefreshToken.Should().Be("refresh-token-new");
        result.Data.User.Should().NotBeNull();
        result.Data.User!.Email.Should().Be("newmail@example.com");
        user.Email.Should().Be("newmail@example.com");
        user.EmailHash.Should().Be("new-email-hash");
        user.PendingEmail.Should().BeNull();
        user.EmailChangeToken.Should().BeNull();
        user.EmailChangeTokenExpiry.Should().BeNull();

        _refreshTokenDalMock.Verify(
            x => x.Update(It.Is<RefreshToken>(token => token.UserId == 61 && token.IsRevoked)),
            Times.AtLeastOnce);
        _emailNotificationServiceMock.Verify(
            x => x.SendAsync(
                "old@example.com",
                It.Is<string>(subject => subject.Contains("değiştirildi", StringComparison.OrdinalIgnoreCase)),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
