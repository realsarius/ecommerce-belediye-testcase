using EcommerceAPI.Entities.DTOs;
using FluentValidation;

namespace EcommerceAPI.Business.Validators;

public class CreateContactMessageRequestValidator : AbstractValidator<CreateContactMessageRequest>
{
    public CreateContactMessageRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Ad soyad zorunludur.")
            .MaximumLength(100).WithMessage("Ad soyad en fazla 100 karakter olabilir.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("E-posta adresi zorunludur.")
            .EmailAddress().WithMessage("Geçerli bir e-posta adresi girin.")
            .MaximumLength(200).WithMessage("E-posta adresi en fazla 200 karakter olabilir.");

        RuleFor(x => x.Subject)
            .NotEmpty().WithMessage("Konu zorunludur.")
            .MaximumLength(120).WithMessage("Konu en fazla 120 karakter olabilir.");

        RuleFor(x => x.Message)
            .NotEmpty().WithMessage("Mesaj zorunludur.")
            .MinimumLength(10).WithMessage("Mesaj en az 10 karakter olmalıdır.")
            .MaximumLength(4000).WithMessage("Mesaj en fazla 4000 karakter olabilir.");
    }
}
