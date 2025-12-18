using System.ComponentModel.DataAnnotations;

namespace EcommerceAPI.Core.DTOs;

public class LoginRequest
{
    [Required(ErrorMessage = "Email alanı zorunludur")]
    [EmailAddress(ErrorMessage = "Geçerli bir email adresi giriniz")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Şifre alanı zorunludur")]
    public string Password { get; set; } = string.Empty;
}
