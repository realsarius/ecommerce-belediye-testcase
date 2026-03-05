using EcommerceAPI.Entities.DTOs;
using FluentValidation;

namespace EcommerceAPI.Business.Validators;

public class VerifyEmailRequestValidator : AbstractValidator<VerifyEmailRequest>
{
    public VerifyEmailRequestValidator()
    {
        RuleFor(x => x.Token)
            .NotEmpty().WithMessage("Doğrulama tokenı zorunludur");
    }
}
