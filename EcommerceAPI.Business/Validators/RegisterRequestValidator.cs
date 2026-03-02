using FluentValidation;
using EcommerceAPI.Entities.DTOs;

namespace EcommerceAPI.Business.Validators;

public class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public RegisterRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email alanı zorunludur")
            .EmailAddress().WithMessage("Geçerli bir email adresi giriniz");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Şifre alanı zorunludur")
            .MinimumLength(6).WithMessage("Şifre en az 6 karakter olmalıdır");

        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("Ad alanı zorunludur");

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("Soyad alanı zorunludur");

        RuleFor(x => x.ReferralCode)
            .MaximumLength(32)
            .When(x => !string.IsNullOrWhiteSpace(x.ReferralCode))
            .WithMessage("Referral kodu en fazla 32 karakter olabilir.");
    }
}
