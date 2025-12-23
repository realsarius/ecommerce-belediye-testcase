using EcommerceAPI.Core.Entities;
using System.ComponentModel.DataAnnotations;

namespace EcommerceAPI.Entities.DTOs;

public class RefreshTokenRequest : IDto
{
    [Required]
    public string RefreshToken { get; set; } = string.Empty;
}
