using EcommerceAPI.Entities.DTOs;
using FluentValidation;

namespace EcommerceAPI.Business.Validators;

public class UpsertWishlistPriceAlertRequestValidator : AbstractValidator<UpsertWishlistPriceAlertRequest>
{
    public UpsertWishlistPriceAlertRequestValidator()
    {
        RuleFor(x => x.ProductId)
            .GreaterThan(0).WithMessage("Geçerli bir ürün seçmelisiniz.");

        RuleFor(x => x.TargetPrice)
            .GreaterThan(0).WithMessage("Hedef fiyat 0'dan büyük olmalıdır.");
    }
}
