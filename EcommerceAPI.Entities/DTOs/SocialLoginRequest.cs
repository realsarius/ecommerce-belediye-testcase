using EcommerceAPI.Core.Entities;

namespace EcommerceAPI.Entities.DTOs;

public class SocialLoginRequest : IDto
{
    public string Provider { get; set; } = string.Empty;
    public string IdToken { get; set; } = string.Empty;
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
}
