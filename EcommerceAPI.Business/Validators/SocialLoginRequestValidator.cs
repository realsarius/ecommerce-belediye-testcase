using EcommerceAPI.Entities.DTOs;
using FluentValidation;

namespace EcommerceAPI.Business.Validators;

public class SocialLoginRequestValidator : AbstractValidator<SocialLoginRequest>
{
    public SocialLoginRequestValidator()
    {
        RuleFor(x => x.Provider)
            .NotEmpty()
            .Must(provider => provider.Equals("google", StringComparison.OrdinalIgnoreCase)
                              || provider.Equals("apple", StringComparison.OrdinalIgnoreCase))
            .WithMessage("Desteklenmeyen sosyal giriş sağlayıcısı.");

        RuleFor(x => x.IdToken)
            .NotEmpty()
            .WithMessage("Kimlik doğrulama tokenı gereklidir.");
    }
}
