using EcommerceAPI.Core.DTOs;

namespace EcommerceAPI.Business.Services.Abstract;

public interface IAuthService
{
    Task<AuthResponse> RegisterAsync(RegisterRequest request);
    Task<AuthResponse> LoginAsync(LoginRequest request);
}
