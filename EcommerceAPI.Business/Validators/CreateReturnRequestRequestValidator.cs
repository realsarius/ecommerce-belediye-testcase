using EcommerceAPI.Entities.DTOs;
using FluentValidation;

namespace EcommerceAPI.Business.Validators;

public class CreateReturnRequestRequestValidator : AbstractValidator<CreateReturnRequestRequest>
{
    public CreateReturnRequestRequestValidator()
    {
        RuleFor(x => x.Type)
            .NotEmpty().WithMessage("Talep tipi zorunludur.")
            .Must(type => type.Equals("Cancellation", StringComparison.OrdinalIgnoreCase) ||
                          type.Equals("Return", StringComparison.OrdinalIgnoreCase))
            .WithMessage("Talep tipi Cancellation veya Return olmalıdır.");

        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage("Talep nedeni zorunludur.")
            .MaximumLength(250).WithMessage("Talep nedeni en fazla 250 karakter olabilir.");

        RuleFor(x => x.RequestNote)
            .MaximumLength(1000).WithMessage("Talep notu en fazla 1000 karakter olabilir.");
    }
}
