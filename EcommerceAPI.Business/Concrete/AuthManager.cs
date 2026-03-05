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
using System.Net;
using System.Security.Cryptography;

namespace EcommerceAPI.Business.Concrete;

public class AuthManager : IAuthService
{
    private const int EmailVerificationCodeLength = 6;
    private const int EmailVerificationCodeMaxAttempts = 5;
    private static readonly TimeSpan EmailVerificationCodeLifetime = TimeSpan.FromMinutes(10);
    private static readonly TimeSpan EmailVerificationCodeLockDuration = TimeSpan.FromMinutes(10);

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
        var rawVerificationCode = GenerateVerificationCode();

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
            EmailVerificationTokenExpiry = DateTime.UtcNow.AddHours(24),
            EmailVerificationCodeHash = _hashingService.Hash(rawVerificationCode),
            EmailVerificationCodeExpiry = DateTime.UtcNow.Add(EmailVerificationCodeLifetime),
            EmailVerificationCodeAttemptCount = 0,
            EmailVerificationCodeLastSentAt = DateTime.UtcNow,
            EmailVerificationCodeLockedUntil = null
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

        await SendEmailVerificationAsync(user, rawVerificationToken, rawVerificationCode);

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
            ClearEmailVerificationArtifacts(user);
            _userDal.Update(user);
            await _unitOfWork.SaveChangesAsync();

            return new ErrorDataResult<AuthResponse>(
                new AuthResponse { Success = false, Message = "Doğrulama linkinin süresi dolmuş." },
                "Doğrulama linkinin süresi dolmuş.",
                ErrorCodes.ExpiredToken);
        }

        user.IsEmailVerified = true;
        ClearEmailVerificationArtifacts(user);
        _userDal.Update(user);

        await _unitOfWork.SaveChangesAsync();

        var response = await IssueAuthResponseAsync(user, "VerifyEmail", Messages.EmailVerified);
        return new SuccessDataResult<AuthResponse>(response, Messages.EmailVerified);
    }

    [ValidationAspect(typeof(VerifyEmailCodeRequestValidator))]
    public async Task<IDataResult<AuthResponse>> VerifyEmailCodeAsync(VerifyEmailCodeRequest request)
    {
        var normalizedEmail = request.Email.ToLowerInvariant().Trim();
        var emailHash = _hashingService.Hash(normalizedEmail);
        var user = (await _userDal.GetListAsync(u => u.EmailHash == emailHash)).FirstOrDefault();

        if (user == null || user.IsEmailVerified)
        {
            return new ErrorDataResult<AuthResponse>(
                new AuthResponse { Success = false, Message = "Doğrulama kodu geçersiz." },
                "Doğrulama kodu geçersiz.",
                ErrorCodes.InvalidCode);
        }

        if (user.EmailVerificationCodeLockedUntil.HasValue && user.EmailVerificationCodeLockedUntil.Value > DateTime.UtcNow)
        {
            var retryAfterSeconds = (int)Math.Ceiling((user.EmailVerificationCodeLockedUntil.Value - DateTime.UtcNow).TotalSeconds);
            return new ErrorDataResult<AuthResponse>(
                new AuthResponse { Success = false, Message = "Çok fazla hatalı deneme yaptınız. Lütfen daha sonra tekrar deneyin." },
                "Çok fazla hatalı deneme yaptınız. Lütfen daha sonra tekrar deneyin.",
                ErrorCodes.TooManyAttempts,
                new { RetryAfterSeconds = Math.Max(1, retryAfterSeconds) });
        }

        if (!user.EmailVerificationCodeExpiry.HasValue || user.EmailVerificationCodeExpiry.Value < DateTime.UtcNow)
        {
            ClearEmailVerificationCodeArtifacts(user);
            _userDal.Update(user);
            await _unitOfWork.SaveChangesAsync();

            return new ErrorDataResult<AuthResponse>(
                new AuthResponse { Success = false, Message = "Doğrulama kodunun süresi dolmuş." },
                "Doğrulama kodunun süresi dolmuş.",
                ErrorCodes.ExpiredCode);
        }

        var incomingCodeHash = _hashingService.Hash(request.Code.Trim());
        if (string.IsNullOrWhiteSpace(user.EmailVerificationCodeHash) ||
            !string.Equals(user.EmailVerificationCodeHash, incomingCodeHash, StringComparison.Ordinal))
        {
            user.EmailVerificationCodeAttemptCount += 1;
            if (user.EmailVerificationCodeAttemptCount >= EmailVerificationCodeMaxAttempts)
            {
                user.EmailVerificationCodeLockedUntil = DateTime.UtcNow.Add(EmailVerificationCodeLockDuration);
            }

            _userDal.Update(user);
            await _unitOfWork.SaveChangesAsync();

            if (user.EmailVerificationCodeLockedUntil.HasValue && user.EmailVerificationCodeLockedUntil.Value > DateTime.UtcNow)
            {
                var retryAfterSeconds = (int)Math.Ceiling((user.EmailVerificationCodeLockedUntil.Value - DateTime.UtcNow).TotalSeconds);
                return new ErrorDataResult<AuthResponse>(
                    new AuthResponse { Success = false, Message = "Çok fazla hatalı deneme yaptınız. Lütfen daha sonra tekrar deneyin." },
                    "Çok fazla hatalı deneme yaptınız. Lütfen daha sonra tekrar deneyin.",
                    ErrorCodes.TooManyAttempts,
                    new { RetryAfterSeconds = Math.Max(1, retryAfterSeconds) });
            }

            return new ErrorDataResult<AuthResponse>(
                new AuthResponse { Success = false, Message = "Doğrulama kodu geçersiz." },
                "Doğrulama kodu geçersiz.",
                ErrorCodes.InvalidCode,
                new { RemainingAttempts = Math.Max(0, EmailVerificationCodeMaxAttempts - user.EmailVerificationCodeAttemptCount) });
        }

        user.IsEmailVerified = true;
        ClearEmailVerificationArtifacts(user);
        _userDal.Update(user);
        await _unitOfWork.SaveChangesAsync();

        var response = await IssueAuthResponseAsync(user, "VerifyEmailCode", Messages.EmailVerified);
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
        var rawCode = GenerateVerificationCode();
        SetEmailVerificationArtifacts(user, rawToken, rawCode);

        _userDal.Update(user);
        await _unitOfWork.SaveChangesAsync();

        await SendEmailVerificationAsync(user, rawToken, rawCode);

        return new SuccessResult(Messages.VerificationEmailSent);
    }

    [ValidationAspect(typeof(ResendVerificationCodeRequestValidator))]
    public async Task<IResult> ResendVerificationCodeAsync(ResendVerificationCodeRequest request)
    {
        var normalizedEmail = request.Email.ToLowerInvariant().Trim();
        var emailHash = _hashingService.Hash(normalizedEmail);

        var rateLimit = await _authRateLimitService.TryConsumeResendVerificationCodeAsync(emailHash);
        if (!rateLimit.Allowed)
        {
            return new ErrorResult(
                "Çok sık doğrulama isteği gönderiyorsunuz. Lütfen biraz bekleyin.",
                ErrorCodes.RateLimitExceeded,
                new { RetryAfterSeconds = rateLimit.RetryAfterSeconds });
        }

        var user = (await _userDal.GetListAsync(u => u.EmailHash == emailHash)).FirstOrDefault();
        if (user == null || user.IsEmailVerified)
        {
            return new SuccessResult(Messages.VerificationCodeSent);
        }

        var rawToken = TokenGenerator.GenerateUrlSafeToken();
        var rawCode = GenerateVerificationCode();
        SetEmailVerificationArtifacts(user, rawToken, rawCode);

        _userDal.Update(user);
        await _unitOfWork.SaveChangesAsync();

        await SendEmailVerificationAsync(user, rawToken, rawCode);

        return new SuccessResult(Messages.VerificationCodeSent);
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

    private void SetEmailVerificationArtifacts(User user, string rawToken, string rawCode)
    {
        user.EmailVerificationToken = _hashingService.Hash(rawToken);
        user.EmailVerificationTokenExpiry = DateTime.UtcNow.AddHours(24);
        user.EmailVerificationCodeHash = _hashingService.Hash(rawCode);
        user.EmailVerificationCodeExpiry = DateTime.UtcNow.Add(EmailVerificationCodeLifetime);
        user.EmailVerificationCodeAttemptCount = 0;
        user.EmailVerificationCodeLastSentAt = DateTime.UtcNow;
        user.EmailVerificationCodeLockedUntil = null;
    }

    private static string GenerateVerificationCode()
    {
        var code = RandomNumberGenerator.GetInt32(0, (int)Math.Pow(10, EmailVerificationCodeLength));
        return code.ToString($"D{EmailVerificationCodeLength}");
    }

    private static void ClearEmailVerificationCodeArtifacts(User user)
    {
        user.EmailVerificationCodeHash = null;
        user.EmailVerificationCodeExpiry = null;
        user.EmailVerificationCodeAttemptCount = 0;
        user.EmailVerificationCodeLastSentAt = null;
        user.EmailVerificationCodeLockedUntil = null;
    }

    private static void ClearEmailVerificationArtifacts(User user)
    {
        user.EmailVerificationToken = null;
        user.EmailVerificationTokenExpiry = null;
        ClearEmailVerificationCodeArtifacts(user);
    }

    private async Task SendEmailVerificationAsync(User user, string rawToken, string rawCode)
    {
        var verificationLink = $"{GetFrontendBaseUrl()}/verify-email?token={Uri.EscapeDataString(rawToken)}";
        var emailSent = await _emailNotificationService.SendAsync(
            user.Email,
            "E-posta adresinizi doğrulayın",
            BuildVerificationEmailBody(user, verificationLink, rawCode));

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

    private static string BuildVerificationEmailBody(User user, string verificationLink, string verificationCode)
    {
        var fullName = GetFullName(user);
        return BuildEmailLayout(
            "E-posta doğrulama",
            "Hesabınızı doğrulayın",
            "Alışverişe devam edebilmek için e-posta doğrulaması gerekiyor",
            $"""
             <p style="margin:0 0 14px;color:#cbd5e1;font-size:15px;line-height:1.7;">
               Merhaba <strong style="color:#f8fafc;">{Encode(fullName)}</strong>, hesabınızı doğrulamak için aşağıdaki butonu kullanın.
             </p>
             {BuildPrimaryButton(verificationLink, "E-posta adresimi doğrula")}
             <p style="margin:0 0 10px;color:#94a3b8;font-size:13px;line-height:1.6;">
               Buton açılmazsa bu doğrulama kodunu kullanabilirsiniz
             </p>
             {BuildCodeBadge(verificationCode)}
             <p style="margin:14px 0 0;color:#94a3b8;font-size:13px;line-height:1.7;">
               Kod <strong style="color:#f8fafc;">10 dakika</strong>, doğrulama bağlantısı ise <strong style="color:#f8fafc;">24 saat</strong> geçerlidir.
             </p>
             {BuildLinkFallback(verificationLink)}
             {BuildSecurityNotice("Bu talebi siz yapmadıysanız bu e-postayı yok sayabilirsiniz")}
             """);
    }

    private static string BuildPasswordResetEmailBody(User user, string resetLink)
    {
        var fullName = GetFullName(user);
        return BuildEmailLayout(
            "Şifre güvenliği",
            "Şifre sıfırlama talebi",
            "Hesabınız için yeni bir şifre belirlemek üzere bağlantıyı kullanın",
            $"""
             <p style="margin:0 0 14px;color:#cbd5e1;font-size:15px;line-height:1.7;">
               Merhaba <strong style="color:#f8fafc;">{Encode(fullName)}</strong>, şifrenizi sıfırlamak için aşağıdaki butona tıklayın.
             </p>
             {BuildPrimaryButton(resetLink, "Şifremi sıfırla")}
             <p style="margin:14px 0 0;color:#94a3b8;font-size:13px;line-height:1.7;">
               Bu bağlantı <strong style="color:#f8fafc;">1 saat</strong> geçerlidir.
             </p>
             {BuildLinkFallback(resetLink)}
             {BuildSecurityNotice("Bu işlemi siz başlatmadıysanız hesabınızın şifresini hemen değiştirin")}
             """);
    }

    private static string BuildPasswordChangedEmailBody(User user)
    {
        var fullName = GetFullName(user);
        return BuildEmailLayout(
            "Hesap bildirimi",
            "Şifreniz başarıyla değiştirildi",
            "Bu e-posta bilgilendirme amaçlı gönderilmiştir",
            $"""
             <p style="margin:0 0 12px;color:#cbd5e1;font-size:15px;line-height:1.7;">
               Merhaba <strong style="color:#f8fafc;">{Encode(fullName)}</strong>, hesabınızın şifresi başarıyla güncellendi.
             </p>
             {BuildSecurityNotice("Bu işlemi siz yapmadıysanız hemen destek ekibimizle iletişime geçin")}
             """);
    }

    private static string BuildEmailChangeVerificationBody(User user, string newEmail, string verificationLink)
    {
        var fullName = GetFullName(user);
        return BuildEmailLayout(
            "E-posta değişikliği",
            "Yeni e-posta adresinizi doğrulayın",
            "Hesap güvenliği için adres değişikliği onayı gerekiyor",
            $"""
             <p style="margin:0 0 14px;color:#cbd5e1;font-size:15px;line-height:1.7;">
               Merhaba <strong style="color:#f8fafc;">{Encode(fullName)}</strong>, hesabınızdaki e-posta adresini
               <strong style="color:#f8fafc;"> {Encode(newEmail)}</strong> olarak değiştirme talebi aldık.
             </p>
             {BuildPrimaryButton(verificationLink, "Yeni e-posta adresimi doğrula")}
             <p style="margin:14px 0 0;color:#94a3b8;font-size:13px;line-height:1.7;">
               Bu doğrulama bağlantısı <strong style="color:#f8fafc;">24 saat</strong> geçerlidir.
             </p>
             {BuildLinkFallback(verificationLink)}
             {BuildSecurityNotice("Bu değişiklik size ait değilse hesabınızı hemen güvene alın")}
             """);
    }

    private static string BuildEmailChangedNotificationBody(User user, string oldEmail, string newEmail)
    {
        var fullName = GetFullName(user);
        return BuildEmailLayout(
            "Hesap bildirimi",
            "E-posta adresiniz güncellendi",
            "Aşağıdaki değişiklik hesabınıza başarıyla uygulandı",
            $"""
             <p style="margin:0 0 14px;color:#cbd5e1;font-size:15px;line-height:1.7;">
               Merhaba <strong style="color:#f8fafc;">{Encode(fullName)}</strong>, hesap e-posta adresiniz güncellendi.
             </p>
             <table role="presentation" width="100%" cellspacing="0" cellpadding="0" style="margin:0 0 8px;border-collapse:collapse;">
               <tr>
                 <td style="padding:10px 12px;background:#111827;border:1px solid #243247;border-radius:10px;color:#cbd5e1;font-size:13px;">
                   <div style="margin:0 0 6px;color:#94a3b8;">Eski e-posta</div>
                   <div style="margin:0;color:#f8fafc;font-weight:600;">{Encode(oldEmail)}</div>
                 </td>
               </tr>
             </table>
             <table role="presentation" width="100%" cellspacing="0" cellpadding="0" style="margin:0;border-collapse:collapse;">
               <tr>
                 <td style="padding:10px 12px;background:#111827;border:1px solid #243247;border-radius:10px;color:#cbd5e1;font-size:13px;">
                   <div style="margin:0 0 6px;color:#94a3b8;">Yeni e-posta</div>
                   <div style="margin:0;color:#f8fafc;font-weight:600;">{Encode(newEmail)}</div>
                 </td>
               </tr>
             </table>
             {BuildSecurityNotice("Bu işlemi siz yapmadıysanız lütfen hemen destek ekibimizle iletişime geçin")}
             """);
    }

    private static string BuildEmailLayout(string section, string title, string subtitle, string content)
    {
        return $"""
                <!doctype html>
                <html lang="tr">
                <head>
                  <meta charset="utf-8" />
                  <meta name="viewport" content="width=device-width, initial-scale=1" />
                  <title>{Encode(title)}</title>
                </head>
                <body style="margin:0;padding:0;background:#070b12;font-family:'Segoe UI',Roboto,Arial,sans-serif;">
                  <table role="presentation" width="100%" cellspacing="0" cellpadding="0" style="background:#070b12;padding:26px 10px;">
                    <tr>
                      <td align="center">
                        <table role="presentation" width="100%" cellspacing="0" cellpadding="0" style="max-width:620px;border:1px solid #1f2937;border-radius:16px;background:#0b1220;overflow:hidden;">
                          <tr>
                            <td style="padding:22px 26px 18px;border-bottom:1px solid #1f2937;background:#0f172a;">
                              <div style="margin:0 0 10px;">
                                <span style="display:inline-block;padding:5px 10px;border-radius:999px;border:1px solid #21405f;background:#0b2540;color:#7dd3fc;font-size:11px;font-weight:700;letter-spacing:.2px;text-transform:uppercase;">
                                  {Encode(section)}
                                </span>
                              </div>
                              <h1 style="margin:0;color:#f8fafc;font-size:24px;line-height:1.3;">{Encode(title)}</h1>
                              <p style="margin:10px 0 0;color:#94a3b8;font-size:14px;line-height:1.6;">{Encode(subtitle)}</p>
                            </td>
                          </tr>
                          <tr>
                            <td style="padding:22px 26px 26px;">
                              {content}
                            </td>
                          </tr>
                        </table>
                        <p style="max-width:620px;margin:14px auto 0;color:#64748b;font-size:12px;line-height:1.7;text-align:center;">
                          Bu e-posta sistem tarafından otomatik olarak gönderildi
                        </p>
                        <p style="max-width:620px;margin:6px auto 0;color:#475569;font-size:12px;line-height:1.7;text-align:center;">
                          © 2026 E-Ticaret
                        </p>
                      </td>
                    </tr>
                  </table>
                </body>
                </html>
                """;
    }

    private static string BuildPrimaryButton(string href, string label)
    {
        return $"""
                <table role="presentation" cellspacing="0" cellpadding="0" style="margin:0 0 16px;border-collapse:collapse;">
                  <tr>
                    <td>
                      <a href="{Encode(href)}"
                         style="display:inline-block;padding:12px 20px;border-radius:10px;background:#1d4ed8;border:1px solid #3b82f6;color:#ffffff;font-size:14px;font-weight:700;text-decoration:none;">
                        {Encode(label)}
                      </a>
                    </td>
                  </tr>
                </table>
                """;
    }

    private static string BuildCodeBadge(string code)
    {
        return $"""
                <table role="presentation" cellspacing="0" cellpadding="0" style="margin:0;border-collapse:collapse;">
                  <tr>
                    <td style="padding:12px 16px;background:#111827;border:1px solid #29374e;border-radius:10px;">
                      <span style="display:inline-block;font-family:'SFMono-Regular',Consolas,Monaco,monospace;font-size:24px;letter-spacing:6px;color:#f8fafc;font-weight:700;">
                        {Encode(code)}
                      </span>
                    </td>
                  </tr>
                </table>
                """;
    }

    private static string BuildLinkFallback(string url)
    {
        return $"""
                <p style="margin:14px 0 0;color:#64748b;font-size:12px;line-height:1.7;word-break:break-all;">
                  Link açılmazsa tarayıcıya yapıştırın: <a href="{Encode(url)}" style="color:#60a5fa;text-decoration:none;">{Encode(url)}</a>
                </p>
                """;
    }

    private static string BuildSecurityNotice(string text)
    {
        return $"""
                <table role="presentation" width="100%" cellspacing="0" cellpadding="0" style="margin:16px 0 0;border-collapse:collapse;">
                  <tr>
                    <td style="padding:11px 12px;background:#241b1b;border:1px solid #5f2c2c;border-radius:10px;color:#fca5a5;font-size:12px;line-height:1.7;">
                      Güvenlik notu: {Encode(text)}
                    </td>
                  </tr>
                </table>
                """;
    }

    private static string Encode(string value)
    {
        return WebUtility.HtmlEncode(value);
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
