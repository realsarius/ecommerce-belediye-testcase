using EcommerceAPI.Entities.DTOs;
using FluentValidation;

namespace EcommerceAPI.Business.Validators;

public class ReorderProductImagesRequestValidator : AbstractValidator<ReorderProductImagesRequest>
{
    public ReorderProductImagesRequestValidator()
    {
        RuleFor(x => x.ImageOrders)
            .NotNull().WithMessage("Görsel sıralama listesi zorunludur")
            .Must(x => x.Count > 0).WithMessage("En az bir görsel gönderilmelidir");

        RuleForEach(x => x.ImageOrders).ChildRules(item =>
        {
            item.RuleFor(x => x.ImageId)
                .GreaterThan(0).WithMessage("ImageId geçersiz");

            item.RuleFor(x => x.DisplayOrder)
                .GreaterThanOrEqualTo(0).WithMessage("DisplayOrder negatif olamaz");
        });
    }
}
