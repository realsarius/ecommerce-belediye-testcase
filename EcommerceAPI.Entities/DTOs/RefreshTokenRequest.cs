using EcommerceAPI.Core.Entities;

namespace EcommerceAPI.Entities.DTOs;

public class RefreshTokenRequest : IDto
{
    public string RefreshToken { get; set; } = string.Empty;
}
