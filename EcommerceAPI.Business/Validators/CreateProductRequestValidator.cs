using FluentValidation;
using EcommerceAPI.Entities.DTOs;

namespace EcommerceAPI.Business.Validators;

public class CreateProductRequestValidator : AbstractValidator<CreateProductRequest>
{
    public CreateProductRequestValidator()
    {
        RuleFor(x => x.SellerId)
            .GreaterThan(0)
            .When(x => x.SellerId.HasValue)
            .WithMessage("Satıcı kimliği geçersiz");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Ürün adı zorunludur")
            .MaximumLength(200);

        RuleFor(x => x.Description)
            .MaximumLength(2000);

        RuleFor(x => x.Price)
            .GreaterThan(0).WithMessage("Fiyat 0'dan büyük olmalıdır");

        RuleFor(x => x.SKU)
            .NotEmpty().WithMessage("SKU zorunludur")
            .MaximumLength(50);

        RuleFor(x => x.CategoryId)
            .GreaterThan(0).WithMessage("Kategori zorunludur");

        RuleFor(x => x.InitialStock)
            .GreaterThanOrEqualTo(0).WithMessage("Stok negatif olamaz");

        RuleForEach(x => x.Images).ChildRules(image =>
        {
            image.RuleFor(x => x.ImageUrl)
                .NotEmpty().WithMessage("Görsel URL zorunludur")
                .Must(BeValidUrl).WithMessage("Geçerli bir görsel URL girin");
        });

        RuleForEach(x => x.Variants).ChildRules(variant =>
        {
            variant.RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Varyant adı zorunludur")
                .MaximumLength(100);

            variant.RuleFor(x => x.Value)
                .NotEmpty().WithMessage("Varyant değeri zorunludur")
                .MaximumLength(200);
        });
    }

    private static bool BeValidUrl(string? value)
    {
        return Uri.TryCreate(value, UriKind.Absolute, out _);
    }
}
