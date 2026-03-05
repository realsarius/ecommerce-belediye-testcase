using EcommerceAPI.Business.Abstract;
using EcommerceAPI.Business.Constants;
using EcommerceAPI.Business.Validators;
using EcommerceAPI.Core.Aspects.Autofac.Logging;
using EcommerceAPI.Core.Aspects.Autofac.Validation;
using EcommerceAPI.Core.CrossCuttingConcerns.Logging;
using EcommerceAPI.Core.Interfaces;
using EcommerceAPI.Core.Utilities.Results;
using EcommerceAPI.Core.Utilities.Security;
using EcommerceAPI.DataAccess.Abstract;
using EcommerceAPI.Entities.Concrete;
using EcommerceAPI.Entities.DTOs;
using EcommerceAPI.Entities.Enums;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace EcommerceAPI.Business.Concrete;

public class AuthManager : IAuthService
{
    private readonly IUserDal _userDal;
    private readonly IRoleDal _roleDal;
    private readonly IRefreshTokenDal _refreshTokenDal;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IConfiguration _configuration;
    private readonly IHashingService _hashingService;
    private readonly IAuditService _auditService;
    private readonly ITokenHelper _tokenHelper;
    private readonly ISocialAuthValidator _socialAuthValidator;
    private readonly IReferralService _referralService;
    private readonly IEmailNotificationService _emailNotificationService;
    private readonly IAuthRateLimitService _authRateLimitService;
    private readonly ILogger<AuthManager> _logger;

    public AuthManager(
        IUserDal userDal,
        IRoleDal roleDal,
        IRefreshTokenDal refreshTokenDal,
        IUnitOfWork unitOfWork,
        IConfiguration configuration,
        IHashingService hashingService,
        IAuditService auditService,
        ITokenHelper tokenHelper,
        ISocialAuthValidator socialAuthValidator,
        IReferralService referralService,
        IEmailNotificationService emailNotificationService,
        IAuthRateLimitService authRateLimitService,
        ILogger<AuthManager> logger)
    {
        _userDal = userDal;
        _roleDal = roleDal;
        _refreshTokenDal = refreshTokenDal;
        _unitOfWork = unitOfWork;
        _configuration = configuration;
        _hashingService = hashingService;
        _auditService = auditService;
        _tokenHelper = tokenHelper;
        _socialAuthValidator = socialAuthValidator;
        _referralService = referralService;
        _emailNotificationService = emailNotificationService;
        _authRateLimitService = authRateLimitService;
        _logger = logger;
    }

    [ValidationAspect(typeof(RegisterRequestValidator))]
    public async Task<IDataResult<AuthResponse>> RegisterAsync(RegisterRequest request)
    {
        var referralValidation = await _referralService.ValidateReferralCodeAsync(request.ReferralCode);
        if (!referralValidation.Success)
        {
            return new ErrorDataResult<AuthResponse>(new AuthResponse { Success = false, Message = referralValidation.Message });
        }

        var emailHash = _hashingService.Hash(request.Email.ToLowerInvariant().Trim());

        var existingUsers = await _userDal.GetListAsync(u => u.EmailHash == emailHash);
        if (existingUsers.Any())
        {
            return new ErrorDataResult<AuthResponse>(new AuthResponse { Success = false, Message = Messages.UserAlreadyExists });
        }

        var customerRole = await GetCustomerRoleAsync();
        if (customerRole == null)
        {
            return new ErrorDataResult<AuthResponse>(new AuthResponse { Success = false, Message = "Rol bulunamadı" });
        }

        var rawVerificationToken = TokenGenerator.GenerateUrlSafeToken();

        var user = new User
        {
            Email = request.Email,
            EmailHash = emailHash,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            FirstName = request.FirstName,
            LastName = request.LastName,
            RoleId = customerRole.Id,
            IsEmailVerified = false,
            EmailVerificationToken = _hashingService.Hash(rawVerificationToken),
            EmailVerificationTokenExpiry = DateTime.UtcNow.AddHours(24)
        };

        await _userDal.AddAsync(user);
        await _unitOfWork.SaveChangesAsync();

        var referralSetup = await _referralService.SetupNewUserAsync(user.Id, request.ReferralCode);
        if (!referralSetup.Success)
        {
            return new ErrorDataResult<AuthResponse>(new AuthResponse { Success = false, Message = referralSetup.Message });
        }

        await _unitOfWork.SaveChangesAsync();

        await _auditService.LogActionAsync(
            user.Id.ToString(),
            "Register",
            "User",
            new { UserId = user.Id, Email = user.Email });

        await SendEmailVerificationAsync(user, rawVerificationToken);

        return new SuccessDataResult<AuthResponse>(new AuthResponse
        {
            Success = true,
            Message = Messages.UserRegistered,
            User = new UserDto
            {
                Id = user.Id,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Role = "Customer",
                Status = user.AccountStatus.ToString(),
                LastLoginAt = user.LastLoginAt,
                IsEmailVerified = user.IsEmailVerified
            }
        });
    }

    [ValidationAspect(typeof(LoginRequestValidator))]
    public async Task<IDataResult<AuthResponse>> LoginAsync(LoginRequest request)
    {
        var emailHash = _hashingService.Hash(request.Email.ToLowerInvariant().Trim());

        var users = await _userDal.GetListAsync(u => u.EmailHash == emailHash);
        var user = users.FirstOrDefault();

        if (user == null)
            return new ErrorDataResult<AuthResponse>(new AuthResponse { Success = false, Message = Messages.UserNotFound });

        var accountStatusValidation = ValidateAccountStatus(user);
        if (!accountStatusValidation.Success)
            return new ErrorDataResult<AuthResponse>(new AuthResponse { Success = false, Message = accountStatusValidation.Message });

        if (string.IsNullOrWhiteSpace(user.PasswordHash))
            return new ErrorDataResult<AuthResponse>(new AuthResponse { Success = false, Message = Messages.SocialAccountPasswordLoginNotAllowed });

        if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            return new ErrorDataResult<AuthResponse>(new AuthResponse { Success = false, Message = Messages.PasswordError });

        var response = await IssueAuthResponseAsync(user, "Login", Messages.SuccessfulLogin);
        return new SuccessDataResult<AuthResponse>(response);
    }

    [ValidationAspect(typeof(SocialLoginRequestValidator))]
    public async Task<IDataResult<AuthResponse>> SocialLoginAsync(SocialLoginRequest request)
    {
        var referralValidation = await _referralService.ValidateReferralCodeAsync(request.ReferralCode);
        if (!referralValidation.Success)
        {
            return new ErrorDataResult<AuthResponse>(new AuthResponse
            {
                Success = false,
                Message = referralValidation.Message
            });
        }

        var validationResult = await _socialAuthValidator.ValidateAsync(request.Provider, request.IdToken);
        if (!validationResult.Success)
        {
            return new ErrorDataResult<AuthResponse>(new AuthResponse
            {
                Success = false,
                Message = validationResult.ErrorMessage ?? Messages.SocialLoginFailed
            });
        }

        var normalizedEmail = validationResult.Email.ToLowerInvariant().Trim();
        var emailHash = _hashingService.Hash(normalizedEmail);
        var user = (await _userDal.GetListAsync(u => u.EmailHash == emailHash)).FirstOrDefault();
        var shouldPersist = false;
        var createdNewUser = false;

        if (user == null)
        {
            var customerRole = await GetCustomerRoleAsync();
            if (customerRole == null)
            {
                return new ErrorDataResult<AuthResponse>(new AuthResponse
                {
                    Success = false,
                    Message = "Rol bulunamadı"
                });
            }

            user = new User
            {
                Email = validationResult.Email,
                EmailHash = emailHash,
                PasswordHash = string.Empty,
                FirstName = request.FirstName?.Trim() ?? validationResult.FirstName?.Trim() ?? "Sosyal",
                LastName = request.LastName?.Trim() ?? validationResult.LastName?.Trim() ?? "Kullanıcı",
                RoleId = customerRole.Id,
                IsEmailVerified = validationResult.EmailVerified
            };

            ApplySocialIdentity(user, validationResult.Provider, validationResult.Subject);
            await _userDal.AddAsync(user);
            shouldPersist = true;
            createdNewUser = true;
        }
        else
        {
            var accountStatusValidation = ValidateAccountStatus(user);
            if (!accountStatusValidation.Success)
            {
                return new ErrorDataResult<AuthResponse>(new AuthResponse
                {
                    Success = false,
                    Message = accountStatusValidation.Message
                });
            }

            var socialIdentityUpdate = ApplySocialIdentityIfNeeded(user, validationResult.Provider, validationResult.Subject);
            if (!socialIdentityUpdate.Success)
            {
                return new ErrorDataResult<AuthResponse>(new AuthResponse
                {
                    Success = false,
                    Message = socialIdentityUpdate.ErrorMessage
                });
            }

            shouldPersist = socialIdentityUpdate.Updated;

            if (validationResult.EmailVerified && !user.IsEmailVerified)
            {
                user.IsEmailVerified = true;
                shouldPersist = true;
            }

            if (string.IsNullOrWhiteSpace(user.FirstName) && !string.IsNullOrWhiteSpace(validationResult.FirstName))
            {
                user.FirstName = validationResult.FirstName.Trim();
                shouldPersist = true;
            }

            if (string.IsNullOrWhiteSpace(user.LastName) && !string.IsNullOrWhiteSpace(validationResult.LastName))
            {
                user.LastName = validationResult.LastName.Trim();
                shouldPersist = true;
            }

            if (shouldPersist)
            {
                _userDal.Update(user);
            }
        }

        if (shouldPersist)
        {
            await _unitOfWork.SaveChangesAsync();
        }

        if (createdNewUser)
        {
            var referralSetup = await _referralService.SetupNewUserAsync(user.Id, request.ReferralCode);
            if (!referralSetup.Success)
            {
                return new ErrorDataResult<AuthResponse>(new AuthResponse
                {
                    Success = false,
                    Message = referralSetup.Message
                });
            }

            await _unitOfWork.SaveChangesAsync();
        }

        var response = await IssueAuthResponseAsync(user, "SocialLogin", Messages.SuccessfulLogin);
        return new SuccessDataResult<AuthResponse>(response);
    }

    [ValidationAspect(typeof(VerifyEmailRequestValidator))]
    public async Task<IDataResult<AuthResponse>> VerifyEmailAsync(VerifyEmailRequest request)
    {
        var hashedToken = _hashingService.Hash(request.Token.Trim());
        var user = (await _userDal.GetListAsync(u => u.EmailVerificationToken == hashedToken)).FirstOrDefault();

        if (user == null)
        {
            return new ErrorDataResult<AuthResponse>(
                new AuthResponse { Success = false, Message = "Doğrulama linki geçersiz." },
                "Doğrulama linki geçersiz.",
                ErrorCodes.InvalidToken);
        }

        if (!user.EmailVerificationTokenExpiry.HasValue || user.EmailVerificationTokenExpiry.Value < DateTime.UtcNow)
        {
            user.EmailVerificationToken = null;
            user.EmailVerificationTokenExpiry = null;
            _userDal.Update(user);
            await _unitOfWork.SaveChangesAsync();

            return new ErrorDataResult<AuthResponse>(
                new AuthResponse { Success = false, Message = "Doğrulama linkinin süresi dolmuş." },
                "Doğrulama linkinin süresi dolmuş.",
                ErrorCodes.ExpiredToken);
        }

        user.IsEmailVerified = true;
        user.EmailVerificationToken = null;
        user.EmailVerificationTokenExpiry = null;
        _userDal.Update(user);

        await _unitOfWork.SaveChangesAsync();

        var response = await IssueAuthResponseAsync(user, "VerifyEmail", Messages.EmailVerified);
        return new SuccessDataResult<AuthResponse>(response, Messages.EmailVerified);
    }

    public async Task<IResult> ResendVerificationAsync(int userId)
    {
        var user = await _userDal.GetAsync(u => u.Id == userId);
        if (user == null)
        {
            return new ErrorResult(Messages.UserNotFound);
        }

        if (user.IsEmailVerified)
        {
            return new ErrorResult(Messages.EmailAlreadyVerified);
        }

        var rateLimit = await _authRateLimitService.TryConsumeResendVerificationAsync(userId);
        if (!rateLimit.Allowed)
        {
            return new ErrorResult(
                "Çok sık doğrulama isteği gönderiyorsunuz. Lütfen biraz bekleyin.",
                ErrorCodes.RateLimitExceeded,
                new { RetryAfterSeconds = rateLimit.RetryAfterSeconds });
        }

        var rawToken = TokenGenerator.GenerateUrlSafeToken();
        user.EmailVerificationToken = _hashingService.Hash(rawToken);
        user.EmailVerificationTokenExpiry = DateTime.UtcNow.AddHours(24);

        _userDal.Update(user);
        await _unitOfWork.SaveChangesAsync();

        await SendEmailVerificationAsync(user, rawToken);

        return new SuccessResult(Messages.VerificationEmailSent);
    }

    [ValidationAspect(typeof(ForgotPasswordRequestValidator))]
    public async Task<IResult> ForgotPasswordAsync(ForgotPasswordRequest request)
    {
        var normalizedEmail = request.Email.ToLowerInvariant().Trim();
        var emailHash = _hashingService.Hash(normalizedEmail);

        var rateLimit = await _authRateLimitService.TryConsumeForgotPasswordAsync(emailHash);
        if (!rateLimit.Allowed)
        {
            return new ErrorResult(
                "Çok fazla şifre sıfırlama isteği gönderdiniz. Lütfen daha sonra tekrar deneyin.",
                ErrorCodes.RateLimitExceeded,
                new { RetryAfterSeconds = rateLimit.RetryAfterSeconds });
        }

        var user = (await _userDal.GetListAsync(u => u.EmailHash == emailHash)).FirstOrDefault();
        if (user != null)
        {
            var rawToken = TokenGenerator.GenerateUrlSafeToken();
            user.PasswordResetToken = _hashingService.Hash(rawToken);
            user.PasswordResetTokenExpiry = DateTime.UtcNow.AddHours(1);

            _userDal.Update(user);
            await _unitOfWork.SaveChangesAsync();

            await SendPasswordResetEmailAsync(user, rawToken);
        }

        return new SuccessResult(Messages.PasswordResetLinkSent);
    }

    [ValidationAspect(typeof(ResetPasswordRequestValidator))]
    public async Task<IResult> ResetPasswordAsync(ResetPasswordRequest request)
    {
        var hashedToken = _hashingService.Hash(request.Token.Trim());
        var user = (await _userDal.GetListAsync(u => u.PasswordResetToken == hashedToken)).FirstOrDefault();

        if (user == null)
        {
            return new ErrorResult("Şifre sıfırlama linki geçersiz.", ErrorCodes.InvalidToken);
        }

        if (!user.PasswordResetTokenExpiry.HasValue || user.PasswordResetTokenExpiry.Value < DateTime.UtcNow)
        {
            user.PasswordResetToken = null;
            user.PasswordResetTokenExpiry = null;
            _userDal.Update(user);
            await _unitOfWork.SaveChangesAsync();

            return new ErrorResult("Şifre sıfırlama linkinin süresi dolmuş.", ErrorCodes.ExpiredToken);
        }

        if (!string.Equals(request.NewPassword, request.ConfirmPassword, StringComparison.Ordinal))
        {
            return new ErrorResult("Şifreler eşleşmiyor.", ErrorCodes.PasswordMismatch);
        }

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        user.PasswordResetToken = null;
        user.PasswordResetTokenExpiry = null;
        _userDal.Update(user);

        await RevokeAllRefreshTokensAsync(user.Id, "Password reset");
        await _unitOfWork.SaveChangesAsync();

        await SendPasswordChangedEmailAsync(user);

        return new SuccessResult(Messages.PasswordResetSuccess);
    }

    [ValidationAspect(typeof(ChangeEmailRequestValidator))]
    public async Task<IResult> ChangeEmailAsync(int userId, ChangeEmailRequest request)
    {
        var user = await _userDal.GetAsync(u => u.Id == userId);
        if (user == null)
        {
            return new ErrorResult(Messages.UserNotFound);
        }

        var accountStatusValidation = ValidateAccountStatus(user);
        if (!accountStatusValidation.Success)
        {
            return accountStatusValidation;
        }

        if (string.IsNullOrWhiteSpace(user.PasswordHash))
        {
            return new ErrorResult(Messages.SocialAccountPasswordLoginNotAllowed);
        }

        if (!BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.PasswordHash))
        {
            return new ErrorResult(Messages.CurrentPasswordInvalid, ErrorCodes.CurrentPasswordInvalid);
        }

        var normalizedNewEmail = request.NewEmail.Trim().ToLowerInvariant();
        var normalizedCurrentEmail = user.Email.Trim().ToLowerInvariant();
        if (normalizedCurrentEmail == normalizedNewEmail)
        {
            return new ErrorResult("Yeni e-posta adresi mevcut adresinizle aynı olamaz.");
        }

        var newEmailHash = _hashingService.Hash(normalizedNewEmail);
        var existingUser = (await _userDal.GetListAsync(u => u.EmailHash == newEmailHash && u.Id != userId)).FirstOrDefault();
        if (existingUser != null)
        {
            return new ErrorResult(Messages.EmailAlreadyInUse, ErrorCodes.EmailAlreadyInUse);
        }

        var rawToken = TokenGenerator.GenerateUrlSafeToken();
        user.PendingEmail = request.NewEmail.Trim();
        user.EmailChangeToken = _hashingService.Hash(rawToken);
        user.EmailChangeTokenExpiry = DateTime.UtcNow.AddHours(24);
        _userDal.Update(user);

        await _unitOfWork.SaveChangesAsync();

        await SendEmailChangeVerificationAsync(user, user.PendingEmail, rawToken);

        return new SuccessResult(Messages.EmailChangeVerificationSent);
    }

    [ValidationAspect(typeof(ConfirmEmailChangeRequestValidator))]
    public async Task<IDataResult<AuthResponse>> ConfirmEmailChangeAsync(ConfirmEmailChangeRequest request)
    {
        var hashedToken = _hashingService.Hash(request.Token.Trim());
        var user = (await _userDal.GetListAsync(u => u.EmailChangeToken == hashedToken)).FirstOrDefault();

        if (user == null)
        {
            return new ErrorDataResult<AuthResponse>(
                new AuthResponse { Success = false, Message = "E-posta değişikliği linki geçersiz." },
                "E-posta değişikliği linki geçersiz.",
                ErrorCodes.InvalidToken);
        }

        if (!user.EmailChangeTokenExpiry.HasValue || user.EmailChangeTokenExpiry.Value < DateTime.UtcNow)
        {
            user.PendingEmail = null;
            user.EmailChangeToken = null;
            user.EmailChangeTokenExpiry = null;
            _userDal.Update(user);
            await _unitOfWork.SaveChangesAsync();

            return new ErrorDataResult<AuthResponse>(
                new AuthResponse { Success = false, Message = "E-posta değişikliği linkinin süresi dolmuş." },
                "E-posta değişikliği linkinin süresi dolmuş.",
                ErrorCodes.ExpiredToken);
        }

        if (string.IsNullOrWhiteSpace(user.PendingEmail))
        {
            user.EmailChangeToken = null;
            user.EmailChangeTokenExpiry = null;
            _userDal.Update(user);
            await _unitOfWork.SaveChangesAsync();

            return new ErrorDataResult<AuthResponse>(
                new AuthResponse { Success = false, Message = "Bekleyen e-posta değişikliği bulunamadı." },
                "Bekleyen e-posta değişikliği bulunamadı.",
                ErrorCodes.InvalidToken);
        }

        var normalizedNewEmail = user.PendingEmail.Trim().ToLowerInvariant();
        var newEmailHash = _hashingService.Hash(normalizedNewEmail);
        var existingUser = (await _userDal.GetListAsync(u => u.EmailHash == newEmailHash && u.Id != user.Id)).FirstOrDefault();
        if (existingUser != null)
        {
            user.PendingEmail = null;
            user.EmailChangeToken = null;
            user.EmailChangeTokenExpiry = null;
            _userDal.Update(user);
            await _unitOfWork.SaveChangesAsync();

            return new ErrorDataResult<AuthResponse>(
                new AuthResponse { Success = false, Message = Messages.EmailAlreadyInUse },
                Messages.EmailAlreadyInUse,
                ErrorCodes.EmailAlreadyInUse);
        }

        var accountStatusValidation = ValidateAccountStatus(user);
        if (!accountStatusValidation.Success)
        {
            return new ErrorDataResult<AuthResponse>(new AuthResponse
            {
                Success = false,
                Message = accountStatusValidation.Message
            });
        }

        var oldEmail = user.Email;
        user.Email = user.PendingEmail.Trim();
        user.EmailHash = newEmailHash;
        user.IsEmailVerified = true;
        user.PendingEmail = null;
        user.EmailChangeToken = null;
        user.EmailChangeTokenExpiry = null;
        _userDal.Update(user);

        await RevokeAllRefreshTokensAsync(user.Id, "Email changed");
        await _unitOfWork.SaveChangesAsync();

        await SendEmailChangedNotificationAsync(oldEmail, user.Email, user);

        var response = await IssueAuthResponseAsync(user, "ConfirmEmailChange", Messages.EmailChangeSuccess);
        return new SuccessDataResult<AuthResponse>(response, Messages.EmailChangeSuccess);
    }

    [ValidationAspect(typeof(RefreshTokenRequestValidator))]
    public async Task<IDataResult<AuthResponse>> RefreshTokenAsync(RefreshTokenRequest request)
    {
        var hashedToken = _hashingService.Hash(request.RefreshToken);

        var tokens = await _refreshTokenDal.GetListAsync(rt => rt.Token == hashedToken);
        var existingToken = tokens.FirstOrDefault();

        if (existingToken == null)
            return new ErrorDataResult<AuthResponse>(new AuthResponse { Success = false, Message = Messages.TokenInvalid });

        if (existingToken.IsRevoked)
            return new ErrorDataResult<AuthResponse>(new AuthResponse { Success = false, Message = Messages.TokenInvalid });

        if (existingToken.IsUsed)
        {
            if (!string.IsNullOrEmpty(existingToken.ReplacedByToken))
            {
                var childTokens = await _refreshTokenDal.GetListAsync(rt => rt.Token == existingToken.ReplacedByToken);
                var childToken = childTokens.FirstOrDefault();
                if (childToken != null)
                {
                    childToken.IsRevoked = true;
                    childToken.RevokedReason = "Attempted reuse of ancestor token";
                    _refreshTokenDal.Update(childToken);
                    await _unitOfWork.SaveChangesAsync();
                }
            }
            return new ErrorDataResult<AuthResponse>(new AuthResponse { Success = false, Message = Messages.TokenInvalid + " (Reuse detected)" });
        }

        if (existingToken.ExpiresAt < DateTime.UtcNow)
            return new ErrorDataResult<AuthResponse>(new AuthResponse { Success = false, Message = Messages.TokenExpired });

        var newRefreshToken = _tokenHelper.GenerateRefreshToken();
        var newHashedRefreshToken = _hashingService.Hash(newRefreshToken);

        existingToken.IsUsed = true;
        existingToken.ReplacedByToken = newHashedRefreshToken;
        _refreshTokenDal.Update(existingToken);

        var newRefreshTokenEntity = new RefreshToken
        {
            UserId = existingToken.UserId,
            Token = newHashedRefreshToken,
            JwtId = Guid.NewGuid().ToString(),
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            IsUsed = false,
            IsRevoked = false
        };

        await _refreshTokenDal.AddAsync(newRefreshTokenEntity);

        var user = await _userDal.GetAsync(u => u.Id == existingToken.UserId);

        if (user == null)
            return new ErrorDataResult<AuthResponse>(new AuthResponse { Success = false, Message = Messages.UserNotFound });

        var accountStatusValidation = ValidateAccountStatus(user);
        if (!accountStatusValidation.Success)
            return new ErrorDataResult<AuthResponse>(new AuthResponse { Success = false, Message = accountStatusValidation.Message });

        var role = await _roleDal.GetAsync(r => r.Id == user.RoleId);
        user.LastLoginAt = DateTime.UtcNow;
        _userDal.Update(user);

        var newAccessToken = _tokenHelper.GenerateAccessToken(
            user.Id,
            user.Email,
            role?.Name ?? "Customer",
            user.FirstName,
            user.LastName,
            user.IsEmailVerified);
        var expiry = DateTime.UtcNow.AddMinutes(
            int.Parse(_configuration["JWT_EXPIRATION_MINUTES"] ?? "60"));

        await _unitOfWork.SaveChangesAsync();

        return new SuccessDataResult<AuthResponse>(new AuthResponse
        {
            Success = true,
            Message = Messages.TokenRefreshed,
            Token = newAccessToken,
            RefreshToken = newRefreshToken,
            RefreshTokenExpiration = newRefreshTokenEntity.ExpiresAt,
            ExpiresAt = expiry,
            User = new UserDto
            {
                Id = user.Id,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Role = role?.Name ?? "Customer",
                Status = user.AccountStatus.ToString(),
                LastLoginAt = user.LastLoginAt,
                IsEmailVerified = user.IsEmailVerified
            }
        });
    }

    [LogAspect]
    public async Task<IResult> RevokeTokenAsync(string token)
    {
        var hashedToken = _hashingService.Hash(token);
        var tokens = await _refreshTokenDal.GetListAsync(rt => rt.Token == hashedToken);
        var existingToken = tokens.FirstOrDefault();

        if (existingToken == null)
            return new ErrorResult(Messages.RefreshTokenNotFound);

        existingToken.IsRevoked = true;
        existingToken.RevokedReason = "User logout";
        _refreshTokenDal.Update(existingToken);

        await _unitOfWork.SaveChangesAsync();

        await _auditService.LogActionAsync(
            existingToken.UserId.ToString(),
            "Logout",
            "User",
            new { UserId = existingToken.UserId });

        return new SuccessResult(Messages.TokenRevoked);
    }

    [LogAspect]
    public async Task<IDataResult<UserDto>> GetUserByIdAsync(int userId)
    {
        var user = await _userDal.GetAsync(u => u.Id == userId);

        if (user == null)
            return new ErrorDataResult<UserDto>("Kullanıcı bulunamadı");

        var role = await _roleDal.GetAsync(r => r.Id == user.RoleId);

        return new SuccessDataResult<UserDto>(new UserDto
        {
            Id = user.Id,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Role = role?.Name ?? "Customer",
            Status = user.AccountStatus.ToString(),
            LastLoginAt = user.LastLoginAt,
            IsEmailVerified = user.IsEmailVerified
        });
    }

    private async Task<AuthResponse> IssueAuthResponseAsync(User user, string auditAction, string message)
    {
        var role = await _roleDal.GetAsync(r => r.Id == user.RoleId);
        var roleName = role?.Name ?? "Customer";
        user.LastLoginAt = DateTime.UtcNow;
        _userDal.Update(user);
        var token = _tokenHelper.GenerateAccessToken(user.Id, user.Email, roleName, user.FirstName, user.LastName, user.IsEmailVerified);
        var expiry = DateTime.UtcNow.AddMinutes(
            int.Parse(_configuration["JWT_EXPIRATION_MINUTES"] ?? "60"));

        var refreshToken = _tokenHelper.GenerateRefreshToken();
        var hashedRefreshToken = _hashingService.Hash(refreshToken);

        var refreshTokenEntity = new RefreshToken
        {
            UserId = user.Id,
            Token = hashedRefreshToken,
            JwtId = Guid.NewGuid().ToString(),
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            IsUsed = false,
            IsRevoked = false
        };

        await _refreshTokenDal.AddAsync(refreshTokenEntity);
        await _unitOfWork.SaveChangesAsync();

        await _auditService.LogActionAsync(
            user.Id.ToString(),
            auditAction,
            "User",
            new { UserId = user.Id, Role = roleName });

        return new AuthResponse
        {
            Success = true,
            Message = message,
            Token = token,
            RefreshToken = refreshToken,
            RefreshTokenExpiration = refreshTokenEntity.ExpiresAt,
            ExpiresAt = expiry,
            User = new UserDto
            {
                Id = user.Id,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Role = roleName,
                Status = user.AccountStatus.ToString(),
                LastLoginAt = user.LastLoginAt,
                IsEmailVerified = user.IsEmailVerified
            }
        };
    }

    private async Task<Role?> GetCustomerRoleAsync()
    {
        return (await _roleDal.GetListAsync(r => r.Name == "Customer")).FirstOrDefault();
    }

    private async Task RevokeAllRefreshTokensAsync(int userId, string reason)
    {
        var refreshTokens = await _refreshTokenDal.GetListAsync(token => token.UserId == userId && !token.IsRevoked);
        foreach (var refreshToken in refreshTokens)
        {
            refreshToken.IsRevoked = true;
            refreshToken.RevokedReason = reason;
            _refreshTokenDal.Update(refreshToken);
        }
    }

    private async Task SendEmailVerificationAsync(User user, string rawToken)
    {
        var verificationLink = $"{GetFrontendBaseUrl()}/verify-email?token={Uri.EscapeDataString(rawToken)}";
        var emailSent = await _emailNotificationService.SendAsync(
            user.Email,
            "E-posta adresinizi doğrulayın",
            BuildVerificationEmailBody(user, verificationLink));

        if (!emailSent)
        {
            _logger.LogWarning(
                "Email verification mail could not be delivered. UserId={UserId}, Email={Email}",
                user.Id,
                user.Email);
        }
    }

    private async Task SendPasswordResetEmailAsync(User user, string rawToken)
    {
        var resetLink = $"{GetFrontendBaseUrl()}/reset-password?token={Uri.EscapeDataString(rawToken)}";
        var emailSent = await _emailNotificationService.SendAsync(
            user.Email,
            "Şifrenizi sıfırlayın",
            BuildPasswordResetEmailBody(user, resetLink));

        if (!emailSent)
        {
            _logger.LogWarning(
                "Password reset mail could not be delivered. UserId={UserId}, Email={Email}",
                user.Id,
                user.Email);
        }
    }

    private async Task SendPasswordChangedEmailAsync(User user)
    {
        var emailSent = await _emailNotificationService.SendAsync(
            user.Email,
            "Şifreniz değiştirildi",
            BuildPasswordChangedEmailBody(user));

        if (!emailSent)
        {
            _logger.LogWarning(
                "Password changed mail could not be delivered. UserId={UserId}, Email={Email}",
                user.Id,
                user.Email);
        }
    }

    private async Task SendEmailChangeVerificationAsync(User user, string newEmail, string rawToken)
    {
        var verificationLink = $"{GetFrontendBaseUrl()}/confirm-email-change?token={Uri.EscapeDataString(rawToken)}";
        var emailSent = await _emailNotificationService.SendAsync(
            newEmail,
            "Yeni e-posta adresinizi doğrulayın",
            BuildEmailChangeVerificationBody(user, newEmail, verificationLink));

        if (!emailSent)
        {
            _logger.LogWarning(
                "Email change verification mail could not be delivered. UserId={UserId}, Email={Email}, NewEmail={NewEmail}",
                user.Id,
                user.Email,
                newEmail);
        }
    }

    private async Task SendEmailChangedNotificationAsync(string oldEmail, string newEmail, User user)
    {
        var emailSent = await _emailNotificationService.SendAsync(
            oldEmail,
            "E-posta adresiniz değiştirildi",
            BuildEmailChangedNotificationBody(user, oldEmail, newEmail));

        if (!emailSent)
        {
            _logger.LogWarning(
                "Email changed notification mail could not be delivered. UserId={UserId}, OldEmail={OldEmail}, NewEmail={NewEmail}",
                user.Id,
                oldEmail,
                newEmail);
        }
    }

    private string GetFrontendBaseUrl()
    {
        var configured = _configuration["EMAIL_BASE_URL"];
        if (string.IsNullOrWhiteSpace(configured))
        {
            configured = _configuration["Email:BaseUrl"];
        }

        if (string.IsNullOrWhiteSpace(configured))
        {
            configured = _configuration["Auth:FrontendBaseUrl"];
        }

        if (string.IsNullOrWhiteSpace(configured))
        {
            configured = _configuration["FRONTEND_VITE_SITE_URL"];
        }

        if (string.IsNullOrWhiteSpace(configured))
        {
            configured = "http://localhost:3000";
        }

        return configured.TrimEnd('/');
    }

    private static string BuildVerificationEmailBody(User user, string verificationLink)
    {
        var fullName = GetFullName(user);
        return $"""
                <p>Merhaba {fullName},</p>
                <p>Hesabınızı doğrulamak için aşağıdaki bağlantıya tıklayın:</p>
                <p><a href="{verificationLink}">E-posta adresimi doğrula</a></p>
                <p>Bu bağlantı 24 saat geçerlidir.</p>
                """;
    }

    private static string BuildPasswordResetEmailBody(User user, string resetLink)
    {
        var fullName = GetFullName(user);
        return $"""
                <p>Merhaba {fullName},</p>
                <p>Şifrenizi sıfırlamak için aşağıdaki bağlantıya tıklayın:</p>
                <p><a href="{resetLink}">Şifremi sıfırla</a></p>
                <p>Bu bağlantı 1 saat geçerlidir.</p>
                """;
    }

    private static string BuildPasswordChangedEmailBody(User user)
    {
        var fullName = GetFullName(user);
        return $"""
                <p>Merhaba {fullName},</p>
                <p>Hesabınızın şifresi başarıyla değiştirildi.</p>
                <p>Bu işlemi siz yapmadıysanız lütfen destek ekibimizle iletişime geçin.</p>
                """;
    }

    private static string BuildEmailChangeVerificationBody(User user, string newEmail, string verificationLink)
    {
        var fullName = GetFullName(user);
        return $"""
                <p>Merhaba {fullName},</p>
                <p>Hesabınızdaki e-posta adresini <strong>{newEmail}</strong> olarak değiştirme talebi aldık.</p>
                <p>Değişikliği onaylamak için aşağıdaki bağlantıya tıklayın:</p>
                <p><a href="{verificationLink}">Yeni e-posta adresimi doğrula</a></p>
                <p>Bu bağlantı 24 saat geçerlidir.</p>
                """;
    }

    private static string BuildEmailChangedNotificationBody(User user, string oldEmail, string newEmail)
    {
        var fullName = GetFullName(user);
        return $"""
                <p>Merhaba {fullName},</p>
                <p>Hesabınızın e-posta adresi başarıyla değiştirildi.</p>
                <p>Eski e-posta: <strong>{oldEmail}</strong></p>
                <p>Yeni e-posta: <strong>{newEmail}</strong></p>
                <p>Bu işlemi siz yapmadıysanız lütfen hemen destek ekibimizle iletişime geçin.</p>
                """;
    }

    private static string GetFullName(User user)
    {
        var fullName = $"{user.FirstName} {user.LastName}".Trim();
        return string.IsNullOrWhiteSpace(fullName) ? "Kullanıcı" : fullName;
    }

    private void ApplySocialIdentity(User user, string provider, string subject)
    {
        if (provider.Equals("google", StringComparison.OrdinalIgnoreCase))
        {
            user.GoogleSubject = subject;
            return;
        }

        user.AppleSubject = subject;
    }

    private (bool Success, bool Updated, string ErrorMessage) ApplySocialIdentityIfNeeded(User user, string provider, string subject)
    {
        if (provider.Equals("google", StringComparison.OrdinalIgnoreCase))
        {
            if (!string.IsNullOrWhiteSpace(user.GoogleSubject) && user.GoogleSubject != subject)
            {
                return (false, false, Messages.SocialAccountConflict);
            }

            if (user.GoogleSubject != subject)
            {
                user.GoogleSubject = subject;
                return (true, true, string.Empty);
            }

            return (true, false, string.Empty);
        }

        if (!string.IsNullOrWhiteSpace(user.AppleSubject) && user.AppleSubject != subject)
        {
            return (false, false, Messages.SocialAccountConflict);
        }

        if (user.AppleSubject != subject)
        {
            user.AppleSubject = subject;
            return (true, true, string.Empty);
        }

        return (true, false, string.Empty);
    }

    private static IResult ValidateAccountStatus(User user)
    {
        return user.AccountStatus switch
        {
            UserAccountStatus.Active => new SuccessResult(),
            UserAccountStatus.Suspended => new ErrorResult("Hesabınız geçici olarak askıya alınmıştır."),
            UserAccountStatus.Banned => new ErrorResult("Hesabınız kullanım dışı bırakılmıştır."),
            _ => new ErrorResult("Hesap durumunuz doğrulanamadı.")
        };
    }
}
