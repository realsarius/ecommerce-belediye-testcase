using EcommerceAPI.Entities.DTOs;
using EcommerceAPI.Entities.Concrete;
using EcommerceAPI.DataAccess.Abstract;
using EcommerceAPI.Core.Interfaces;
using EcommerceAPI.Core.Utilities.Results;
using EcommerceAPI.Core.CrossCuttingConcerns.Logging;
using Microsoft.Extensions.Configuration;
using EcommerceAPI.Core.Aspects.Autofac.Validation;
using EcommerceAPI.Business.Validators;
using EcommerceAPI.Core.Aspects.Autofac.Logging;
using EcommerceAPI.Business.Constants;
using EcommerceAPI.Business.Abstract;

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
        IReferralService referralService)
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

        var roles = await _roleDal.GetListAsync(r => r.Name == "Customer");
        var customerRole = roles.FirstOrDefault();
        
        if (customerRole == null)
        {
            return new ErrorDataResult<AuthResponse>(new AuthResponse { Success = false, Message = "Rol bulunamadı" });
        }

        var user = new User
        {
            Email = request.Email,
            EmailHash = emailHash,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            FirstName = request.FirstName,
            LastName = request.LastName,
            RoleId = customerRole.Id
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

        return new SuccessDataResult<AuthResponse>(new AuthResponse { Success = true, Message = Messages.UserRegistered });
    }

    [ValidationAspect(typeof(LoginRequestValidator))]
    public async Task<IDataResult<AuthResponse>> LoginAsync(LoginRequest request)
    {
        var emailHash = _hashingService.Hash(request.Email.ToLowerInvariant().Trim());
        
        var users = await _userDal.GetListAsync(u => u.EmailHash == emailHash);
        var user = users.FirstOrDefault();

        if (user == null)
            return new ErrorDataResult<AuthResponse>(new AuthResponse { Success = false, Message = Messages.UserNotFound });

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
        
        var role = await _roleDal.GetAsync(r => r.Id == user.RoleId);

         var newAccessToken = _tokenHelper.GenerateAccessToken(user.Id, user.Email, role?.Name ?? "Customer", user.FirstName, user.LastName);
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
                Role = role?.Name ?? "Customer"
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

    private async Task<AuthResponse> IssueAuthResponseAsync(User user, string auditAction, string message)
    {
        var role = await _roleDal.GetAsync(r => r.Id == user.RoleId);
        var roleName = role?.Name ?? "Customer";
        var token = _tokenHelper.GenerateAccessToken(user.Id, user.Email, roleName, user.FirstName, user.LastName);
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
                Role = roleName
            }
        };
    }

    private async Task<Role?> GetCustomerRoleAsync()
    {
        return (await _roleDal.GetListAsync(r => r.Name == "Customer")).FirstOrDefault();
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
            Role = role?.Name ?? "Customer"
        });
    }
}
