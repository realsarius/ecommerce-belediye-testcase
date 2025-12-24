using EcommerceAPI.Business.Abstract;
using EcommerceAPI.Entities.DTOs;
using EcommerceAPI.Entities.Concrete;
using EcommerceAPI.Core.Interfaces;
using EcommerceAPI.DataAccess.Abstract;
using EcommerceAPI.Core.Utilities.Results;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace EcommerceAPI.Business.Concrete;

public class AuthManager : IAuthService
{
    private readonly IUserDal _userDal;
    private readonly IRoleDal _roleDal;
    private readonly IRefreshTokenDal _refreshTokenDal;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IConfiguration _configuration;
    private readonly IHashingService _hashingService;

    public AuthManager(
        IUserDal userDal,
        IRoleDal roleDal,
        IRefreshTokenDal refreshTokenDal,
        IUnitOfWork unitOfWork,
        IConfiguration configuration,
        IHashingService hashingService)
    {
        _userDal = userDal;
        _roleDal = roleDal;
        _refreshTokenDal = refreshTokenDal;
        _unitOfWork = unitOfWork;
        _configuration = configuration;
        _hashingService = hashingService;
    }

    public async Task<IDataResult<AuthResponse>> RegisterAsync(RegisterRequest request)
    {
        // Email hash'i oluştur (arama için)
        var emailHash = _hashingService.Hash(request.Email.ToLowerInvariant().Trim());
        
        // Email kontrolü
        // IEntityRepository GetListAsync kullanıyoruz. filter null değil.
        var existingUsers = await _userDal.GetListAsync(u => u.EmailHash == emailHash);
        if (existingUsers.Any())
        {
            return new ErrorDataResult<AuthResponse>(new AuthResponse { Success = false, Message = "Bu email zaten kayıtlı" });
        }

        // Customer rolünü bul
        var roles = await _roleDal.GetListAsync(r => r.Name == "Customer");
        var customerRole = roles.FirstOrDefault();
        
        if (customerRole == null)
        {
            return new ErrorDataResult<AuthResponse>(new AuthResponse { Success = false, Message = "Rol bulunamadı" });
        }

        // Kullanıcı oluştur
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

        return new SuccessDataResult<AuthResponse>(new AuthResponse { Success = true, Message = "Kayıt başarılı" });
    }

    public async Task<IDataResult<AuthResponse>> LoginAsync(LoginRequest request)
    {
        var emailHash = _hashingService.Hash(request.Email.ToLowerInvariant().Trim());
        
        var users = await _userDal.GetListAsync(u => u.EmailHash == emailHash);
        var user = users.FirstOrDefault();

        if (user == null)
            return new ErrorDataResult<AuthResponse>(new AuthResponse { Success = false, Message = "Email veya şifre hatalı" });

        if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
             return new ErrorDataResult<AuthResponse>(new AuthResponse { Success = false, Message = "Email veya şifre hatalı" });

        var role = await _roleDal.GetAsync(r => r.Id == user.RoleId);

        var token = GenerateJwtToken(user, role?.Name ?? "Customer");
        var expiry = DateTime.UtcNow.AddMinutes(
            int.Parse(_configuration["JWT_EXPIRATION_MINUTES"] ?? "60"));

        var refreshToken = GenerateRefreshToken();
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

        return new SuccessDataResult<AuthResponse>(new AuthResponse
        {
            Success = true,
            Message = "Giriş başarılı",
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

    public async Task<IDataResult<AuthResponse>> RefreshTokenAsync(RefreshTokenRequest request)
    {
        var hashedToken = _hashingService.Hash(request.RefreshToken);
        
        var tokens = await _refreshTokenDal.GetListAsync(rt => rt.Token == hashedToken);
        var existingToken = tokens.FirstOrDefault();

        if (existingToken == null)
             return new ErrorDataResult<AuthResponse>(new AuthResponse { Success = false, Message = "Geçersiz token" });

        if (existingToken.IsRevoked)
             return new ErrorDataResult<AuthResponse>(new AuthResponse { Success = false, Message = "Geçersiz token" });

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
                    _refreshTokenDal.Update(childToken); // Explicit update
                    await _unitOfWork.SaveChangesAsync();
                }
            }
            return new ErrorDataResult<AuthResponse>(new AuthResponse { Success = false, Message = "Geçersiz token (Reuse detected)" });
        }

        if (existingToken.ExpiresAt < DateTime.UtcNow)
             return new ErrorDataResult<AuthResponse>(new AuthResponse { Success = false, Message = "Token süresi dolmuş" });

        var newRefreshToken = GenerateRefreshToken();
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
             return new ErrorDataResult<AuthResponse>(new AuthResponse { Success = false, Message = "Kullanıcı bulunamadı" });
        
        var role = await _roleDal.GetAsync(r => r.Id == user.RoleId);

         var newAccessToken = GenerateJwtToken(user, role?.Name ?? "Customer");
         var expiry = DateTime.UtcNow.AddMinutes(
            int.Parse(_configuration["JWT_EXPIRATION_MINUTES"] ?? "60"));

        await _unitOfWork.SaveChangesAsync();

        return new SuccessDataResult<AuthResponse>(new AuthResponse
        {
            Success = true,
            Message = "Token yenilendi",
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

    public async Task<IResult> RevokeTokenAsync(string token)
    {
        var hashedToken = _hashingService.Hash(token);
        var tokens = await _refreshTokenDal.GetListAsync(rt => rt.Token == hashedToken);
        var existingToken = tokens.FirstOrDefault();

        if (existingToken == null)
            return new ErrorResult("Token bulunamadı");

        existingToken.IsRevoked = true;
        existingToken.RevokedReason = "User logout";
        _refreshTokenDal.Update(existingToken);
        
        await _unitOfWork.SaveChangesAsync();
        return new SuccessResult("Token iptal edildi");
    }

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

    private string GenerateRefreshToken()
    {
        var randomNumber = new byte[32];
        using var rng = System.Security.Cryptography.RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return Convert.ToBase64String(randomNumber);
    }

    private string GenerateJwtToken(User user, string role)
    {
        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_configuration["JWT_SECRET_KEY"] ?? "default-key"));
        
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, role),
            new Claim(ClaimTypes.GivenName, user.FirstName),
            new Claim(ClaimTypes.Surname, user.LastName)
        };

        var token = new JwtSecurityToken(
            issuer: _configuration["JWT_ISSUER"],
            audience: _configuration["JWT_AUDIENCE"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(
                int.Parse(_configuration["JWT_EXPIRATION_MINUTES"] ?? "60")),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
