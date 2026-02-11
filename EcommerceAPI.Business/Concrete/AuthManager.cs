using EcommerceAPI.Business.Abstract;
using EcommerceAPI.Entities.DTOs;
using EcommerceAPI.Entities.Concrete;
using EcommerceAPI.Core.Interfaces;
using EcommerceAPI.DataAccess.Abstract;
using EcommerceAPI.Core.Utilities.Results;
using EcommerceAPI.Core.CrossCuttingConcerns.Logging;
using Microsoft.Extensions.Configuration;
using EcommerceAPI.Core.Aspects.Autofac.Validation;
using EcommerceAPI.Business.Validators;
using EcommerceAPI.Core.Aspects.Autofac.Logging;
using EcommerceAPI.Business.Constants;

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

    public AuthManager(
        IUserDal userDal,
        IRoleDal roleDal,
        IRefreshTokenDal refreshTokenDal,
        IUnitOfWork unitOfWork,
        IConfiguration configuration,
        IHashingService hashingService,
        IAuditService auditService,
        ITokenHelper tokenHelper)
    {
        _userDal = userDal;
        _roleDal = roleDal;
        _refreshTokenDal = refreshTokenDal;
        _unitOfWork = unitOfWork;
        _configuration = configuration;
        _hashingService = hashingService;
        _auditService = auditService;
        _tokenHelper = tokenHelper;
    }

    [ValidationAspect(typeof(RegisterRequestValidator))]
    public async Task<IDataResult<AuthResponse>> RegisterAsync(RegisterRequest request)
    {
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

        if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
             return new ErrorDataResult<AuthResponse>(new AuthResponse { Success = false, Message = Messages.PasswordError });

        var role = await _roleDal.GetAsync(r => r.Id == user.RoleId);

        var token = _tokenHelper.GenerateAccessToken(user.Id, user.Email, role?.Name ?? "Customer", user.FirstName, user.LastName);
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
            "Login",
            "User",
            new { UserId = user.Id, Role = role?.Name ?? "Customer" });

        return new SuccessDataResult<AuthResponse>(new AuthResponse
        {
            Success = true,
            Message = Messages.SuccessfulLogin,
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
                Role = role?.Name ?? "Customer"
            }
        });
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
