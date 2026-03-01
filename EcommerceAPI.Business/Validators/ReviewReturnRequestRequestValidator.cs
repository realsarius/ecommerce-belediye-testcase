using EcommerceAPI.Entities.DTOs;
using FluentValidation;

namespace EcommerceAPI.Business.Validators;

public class ReviewReturnRequestRequestValidator : AbstractValidator<ReviewReturnRequestRequest>
{
    public ReviewReturnRequestRequestValidator()
    {
        RuleFor(x => x.Status)
            .NotEmpty().WithMessage("Karar durumu zorunludur.")
            .Must(status => status.Equals("Approved", StringComparison.OrdinalIgnoreCase) ||
                            status.Equals("Rejected", StringComparison.OrdinalIgnoreCase))
            .WithMessage("Karar durumu Approved veya Rejected olmalıdır.");

        RuleFor(x => x.ReviewNote)
            .MaximumLength(1000).WithMessage("İnceleme notu en fazla 1000 karakter olabilir.");
    }
}
