using System.ComponentModel.DataAnnotations;

namespace EcommerceAPI.Core.DTOs;

public class RefreshTokenRequest
{
    [Required]
    public string RefreshToken { get; set; } = string.Empty;
}
