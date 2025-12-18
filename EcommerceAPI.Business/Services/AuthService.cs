using EcommerceAPI.Core.DTOs;
using EcommerceAPI.Core.Entities;
using EcommerceAPI.Core.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace EcommerceAPI.Business.Services;

public class AuthService : IAuthService
{
    private readonly IRepository<User> _userRepository;
    private readonly IRepository<Role> _roleRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IConfiguration _configuration;

    public AuthService(
        IRepository<User> userRepository,
        IRepository<Role> roleRepository,
        IUnitOfWork unitOfWork,
        IConfiguration configuration)
    {
        _userRepository = userRepository;
        _roleRepository = roleRepository;
        _unitOfWork = unitOfWork;
        _configuration = configuration;
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
    {
        // Email kontrolü
        var existingUsers = await _userRepository.FindAsync(u => u.Email == request.Email);
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
            Email = request.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            FirstName = request.FirstName,
            LastName = request.LastName,
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
        // Kullanıcıyı bul
        var users = await _userRepository.FindAsync(u => u.Email == request.Email);
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

        // Token oluştur
        var token = GenerateJwtToken(user, role?.Name ?? "Customer");
        var expiry = DateTime.UtcNow.AddMinutes(
            int.Parse(_configuration["JWT_EXPIRATION_MINUTES"] ?? "60"));

        return new AuthResponse
        {
            Success = true,
            Message = "Giriş başarılı",
            Token = token,
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