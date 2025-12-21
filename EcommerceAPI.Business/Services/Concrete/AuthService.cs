using EcommerceAPI.Business.Services.Abstract;
using EcommerceAPI.Core.DTOs;
using EcommerceAPI.Core.Entities;
using EcommerceAPI.Core.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace EcommerceAPI.Business.Services.Concrete;

public class AuthService : IAuthService
{
    private readonly IRepository<User> _userRepository;
    private readonly IRepository<Role> _roleRepository;
    private readonly IRepository<RefreshToken> _refreshTokenRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IConfiguration _configuration;
    private readonly IHashingService _hashingService;

    public AuthService(
        IRepository<User> userRepository,
        IRepository<Role> roleRepository,
        IRepository<RefreshToken> refreshTokenRepository,
        IUnitOfWork unitOfWork,
        IConfiguration configuration,
        IHashingService hashingService)
    {
        _userRepository = userRepository;
        _roleRepository = roleRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _unitOfWork = unitOfWork;
        _configuration = configuration;
        _hashingService = hashingService;
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
    {
        // Email hash'i oluştur (arama için)
        var emailHash = _hashingService.Hash(request.Email.ToLowerInvariant().Trim());
        
        // Email kontrolü (EmailHash üzerinden)
        var existingUsers = await _userRepository.FindAsync(u => u.EmailHash == emailHash);
        if (existingUsers.Any())
        {
            return new AuthResponse { Success = false, Message = "Bu email zaten kayıtlı" };
        }

        // Customer rolünü bul
        var roles = await _roleRepository.FindAsync(r => r.Name == "Customer");
        var customerRole = roles.FirstOrDefault();
        
        if (customerRole == null)
        {
            return new AuthResponse { Success = false, Message = "Rol bulunamadı" };
        }

        // Kullanıcı oluştur
        var user = new User
        {
            Email = request.Email, // ValueConverter ile otomatik şifrelenir
            EmailHash = emailHash, // Arama için hash
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            FirstName = request.FirstName, // ValueConverter ile otomatik şifrelenir
            LastName = request.LastName,   // ValueConverter ile otomatik şifrelenir
            RoleId = customerRole.Id
        };

        await _userRepository.AddAsync(user);
        await _unitOfWork.SaveChangesAsync();

        return new AuthResponse 
        { 
            Success = true, 
            Message = "Kayıt başarılı" 
        };
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        // Email hash'i oluştur (arama için)
        var emailHash = _hashingService.Hash(request.Email.ToLowerInvariant().Trim());
        
        // Kullanıcıyı bul
        var users = await _userRepository.FindAsync(u => u.EmailHash == emailHash);
        var user = users.FirstOrDefault();

        if (user == null)
        {
            return new AuthResponse { Success = false, Message = "Email veya şifre hatalı" };
        }

        // Şifre kontrolü
        if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            return new AuthResponse { Success = false, Message = "Email veya şifre hatalı" };
        }

        // Rolü al
        var roles = await _roleRepository.FindAsync(r => r.Id == user.RoleId);
        var role = roles.FirstOrDefault();

        // Access Token oluştur
        var token = GenerateJwtToken(user, role?.Name ?? "Customer");
        var expiry = DateTime.UtcNow.AddMinutes(
            int.Parse(_configuration["JWT_EXPIRATION_MINUTES"] ?? "60"));

        // Refresh Token oluştur
        var refreshToken = GenerateRefreshToken();
        var hashedRefreshToken = _hashingService.Hash(refreshToken);
        
        var refreshTokenEntity = new RefreshToken
        {
            UserId = user.Id,
            Token = hashedRefreshToken,
            JwtId = Guid.NewGuid().ToString(), // Access token ile eşleştirilebilir (JTI claim)
            ExpiresAt = DateTime.UtcNow.AddDays(7), // 7 gün
            IsUsed = false,
            IsRevoked = false
        };

        await _refreshTokenRepository.AddAsync(refreshTokenEntity);
        await _unitOfWork.SaveChangesAsync();

        return new AuthResponse
        {
            Success = true,
            Message = "Giriş başarılı",
            Token = token,
            RefreshToken = refreshToken, // Client'a plain text döneriz
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
        };
    }

    public async Task<AuthResponse> RefreshTokenAsync(RefreshTokenRequest request)
    {
        var hashedToken = _hashingService.Hash(request.RefreshToken);
        
        var tokens = await _refreshTokenRepository.FindAsync(rt => rt.Token == hashedToken);
        var existingToken = tokens.FirstOrDefault();

        if (existingToken == null)
        {
            return new AuthResponse { Success = false, Message = "Geçersiz token" };
        }

        if (existingToken.IsRevoked)
        {
            // Revoke edilmiş token kullanılmaya çalışılıyor - Güvenlik sorunu
            return new AuthResponse { Success = false, Message = "Geçersiz token" };
        }

        if (existingToken.IsUsed)
        {
            // Reuse Detection: Kullanılmış token tekrar kullanılıyor
            // Bu bir hırsızlık girişimi olabilir. Token zincirini iptal et.
            if (!string.IsNullOrEmpty(existingToken.ReplacedByToken))
            {
                var childTokens = await _refreshTokenRepository.FindAsync(rt => rt.Token == existingToken.ReplacedByToken);
                var childToken = childTokens.FirstOrDefault();
                if (childToken != null)
                {
                    childToken.IsRevoked = true;
                    childToken.RevokedReason = "Attempted reuse of ancestor token";
                    await _unitOfWork.SaveChangesAsync();
                }
            }
            
            return new AuthResponse { Success = false, Message = "Geçersiz token (Reuse detected)" };
        }

        if (existingToken.ExpiresAt < DateTime.UtcNow)
        {
            return new AuthResponse { Success = false, Message = "Token süresi dolmuş" };
        }

        // Token Rotation
        var newRefreshToken = GenerateRefreshToken();
        var newHashedRefreshToken = _hashingService.Hash(newRefreshToken);

        existingToken.IsUsed = true;
        existingToken.ReplacedByToken = newHashedRefreshToken;
        _refreshTokenRepository.Update(existingToken); // Persist changes

        var newRefreshTokenEntity = new RefreshToken
        {
            UserId = existingToken.UserId,
            Token = newHashedRefreshToken,
            JwtId = Guid.NewGuid().ToString(),
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            IsUsed = false,
            IsRevoked = false
        };

        await _refreshTokenRepository.AddAsync(newRefreshTokenEntity);
        
        // Kullanıcı bilgisini çek (Access Token için)
        // Repo'dan include ile çekemiyorsak tekrar sorgulamalıyız. User repo generic ise include desteklemeyebilir.
        var users = await _userRepository.FindAsync(u => u.Id == existingToken.UserId);
        var user = users.FirstOrDefault();
        
        if (user == null)
        {
             return new AuthResponse { Success = false, Message = "Kullanıcı bulunamadı" };
        }
        
        var roles = await _roleRepository.FindAsync(r => r.Id == user.RoleId);
        var role = roles.FirstOrDefault();

        var newAccessToken = GenerateJwtToken(user, role?.Name ?? "Customer");
         var expiry = DateTime.UtcNow.AddMinutes(
            int.Parse(_configuration["JWT_EXPIRATION_MINUTES"] ?? "60"));

        await _unitOfWork.SaveChangesAsync();

        return new AuthResponse
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
        };
    }

    public async Task<bool> RevokeTokenAsync(string token)
    {
        var hashedToken = _hashingService.Hash(token);
        var tokens = await _refreshTokenRepository.FindAsync(rt => rt.Token == hashedToken);
        var existingToken = tokens.FirstOrDefault();

        if (existingToken == null)
            return false;

        existingToken.IsRevoked = true;
        existingToken.RevokedReason = "User logout";
        _refreshTokenRepository.Update(existingToken); // Persist changes
        
        await _unitOfWork.SaveChangesAsync();
        return true;
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
