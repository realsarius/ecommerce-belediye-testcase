using EcommerceAPI.Core.Entities;

namespace EcommerceAPI.Entities.DTOs;

public class VerifyEmailRequest : IDto
{
    public string Token { get; set; } = string.Empty;
}

public class VerifyEmailCodeRequest : IDto
{
    public string Email { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
}

public class ResendVerificationCodeRequest : IDto
{
    public string Email { get; set; } = string.Empty;
}

public class ForgotPasswordRequest : IDto
{
    public string Email { get; set; } = string.Empty;
}

public class ResetPasswordRequest : IDto
{
    public string Token { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
    public string ConfirmPassword { get; set; } = string.Empty;
}

public class ChangeEmailRequest : IDto
{
    public string NewEmail { get; set; } = string.Empty;
    public string CurrentPassword { get; set; } = string.Empty;
}

public class ConfirmEmailChangeRequest : IDto
{
    public string Token { get; set; } = string.Empty;
}
