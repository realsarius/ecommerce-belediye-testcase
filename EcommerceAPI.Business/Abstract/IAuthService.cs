using EcommerceAPI.Entities.DTOs;
using EcommerceAPI.Core.Utilities.Results;

namespace EcommerceAPI.Business.Abstract;

public interface IAuthService
{
    Task<IDataResult<AuthResponse>> RegisterAsync(RegisterRequest request);
    Task<IDataResult<AuthResponse>> LoginAsync(LoginRequest request);
    Task<IDataResult<AuthResponse>> RefreshTokenAsync(RefreshTokenRequest request);
    Task<IResult> RevokeTokenAsync(string token);
    Task<IDataResult<UserDto>> GetUserByIdAsync(int userId);
}

