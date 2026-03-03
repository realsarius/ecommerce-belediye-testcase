using FluentValidation;
using EcommerceAPI.Entities.DTOs;

namespace EcommerceAPI.Business.Validators;

public class UpdateCategoryRequestValidator : AbstractValidator<UpdateCategoryRequest>
{
    public UpdateCategoryRequestValidator()
    {
        RuleFor(x => x.Name)
            .Length(2, 255).WithMessage("Kategori adı 2-255 karakter arasında olmalıdır")
            .When(x => !string.IsNullOrEmpty(x.Name));

        RuleFor(x => x.Description)
            .MaximumLength(1000).WithMessage("Açıklama en fazla 1000 karakter olabilir");

        RuleFor(x => x.ParentCategoryId)
            .GreaterThan(0).WithMessage("Geçersiz üst kategori seçimi")
            .When(x => x.ParentCategoryId.HasValue);

        RuleFor(x => x.SortOrder)
            .GreaterThanOrEqualTo(0).WithMessage("Sıralama 0 veya daha büyük olmalıdır")
            .When(x => x.SortOrder.HasValue);
    }
}
