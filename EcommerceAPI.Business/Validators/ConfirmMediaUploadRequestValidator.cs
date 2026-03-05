using EcommerceAPI.Entities.DTOs;
using FluentValidation;

namespace EcommerceAPI.Business.Validators;

public class ConfirmMediaUploadRequestValidator : AbstractValidator<ConfirmMediaUploadRequest>
{
    public ConfirmMediaUploadRequestValidator()
    {
        RuleFor(x => x.Context)
            .NotEmpty().WithMessage("Upload context zorunludur")
            .Must(BeKnownContext).WithMessage("Geçersiz upload context");

        RuleFor(x => x.ReferenceId)
            .GreaterThanOrEqualTo(0).WithMessage("ReferenceId negatif olamaz");

        RuleFor(x => x.ObjectKey)
            .NotEmpty().WithMessage("Object key zorunludur")
            .MaximumLength(1024).WithMessage("Object key çok uzun");

        RuleFor(x => x.SortOrder)
            .GreaterThanOrEqualTo(0)
            .When(x => x.SortOrder.HasValue)
            .WithMessage("Sort order negatif olamaz");
    }

    private static bool BeKnownContext(string context)
    {
        var normalized = context.Trim().ToLowerInvariant();
        return normalized is "product" or "category" or "seller-logo" or "seller-banner";
    }
}
