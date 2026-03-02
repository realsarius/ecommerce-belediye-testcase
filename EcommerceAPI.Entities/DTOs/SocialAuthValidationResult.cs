using EcommerceAPI.Core.Entities;

namespace EcommerceAPI.Entities.DTOs;

public class SocialAuthValidationResult : IDto
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public string Provider { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool EmailVerified { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
}
