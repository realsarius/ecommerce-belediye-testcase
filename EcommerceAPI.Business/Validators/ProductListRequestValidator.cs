using FluentValidation;
using EcommerceAPI.Entities.DTOs;

namespace EcommerceAPI.Business.Validators;

public class ProductListRequestValidator : AbstractValidator<ProductListRequest>
{
    public ProductListRequestValidator()
    {
        RuleFor(x => x.Page)
            .GreaterThanOrEqualTo(1).WithMessage("Sayfa numarası en az 1 olmalıdır");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100).WithMessage("Sayfa boyutu 1-100 arasında olmalıdır");

        RuleFor(x => x.MinPrice)
            .GreaterThanOrEqualTo(0).WithMessage("Minimum fiyat negatif olamaz")
            .When(x => x.MinPrice.HasValue);

        RuleFor(x => x.MaxPrice)
            .GreaterThanOrEqualTo(0).WithMessage("Maksimum fiyat negatif olamaz")
            .When(x => x.MaxPrice.HasValue);

        RuleFor(x => x)
            .Must(x => !x.MinPrice.HasValue || !x.MaxPrice.HasValue || x.MinPrice <= x.MaxPrice)
            .WithMessage("Minimum fiyat maksimum fiyattan büyük olamaz");

        RuleFor(x => x.SortBy)
            .Must(sortBy => new[] { "name", "price", "createdat", "wishlistcount" }.Contains(sortBy.ToLower()))
            .WithMessage("Geçersiz sıralama alanı. Geçerli değerler: name, price, createdAt, wishlistCount");
    }
}
