using FluentValidation;
using EcommerceAPI.Entities.DTOs;

namespace EcommerceAPI.Business.Validators;

public class AddToCartRequestValidator : AbstractValidator<AddToCartRequest>
{
    public AddToCartRequestValidator()
    {
        RuleFor(x => x.ProductId)
            .GreaterThan(0).WithMessage("Geçerli bir ürün ID giriniz");

        RuleFor(x => x.Quantity)
            .InclusiveBetween(1, 100).WithMessage("Miktar 1 ile 100 arasında olmalıdır");
    }
}
