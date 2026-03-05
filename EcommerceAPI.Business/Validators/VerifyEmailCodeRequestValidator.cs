using EcommerceAPI.Entities.DTOs;
using FluentValidation;

namespace EcommerceAPI.Business.Validators;

public class VerifyEmailCodeRequestValidator : AbstractValidator<VerifyEmailCodeRequest>
{
    public VerifyEmailCodeRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email alanı zorunludur")
            .EmailAddress().WithMessage("Geçerli bir email adresi giriniz");

        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Doğrulama kodu zorunludur")
            .Length(6).WithMessage("Doğrulama kodu 6 karakter olmalıdır")
            .Matches("^[0-9]{6}$").WithMessage("Doğrulama kodu yalnızca rakamlardan oluşmalıdır");
    }
}
