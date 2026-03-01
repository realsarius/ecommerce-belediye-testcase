using FluentValidation;
using EcommerceAPI.Entities.DTOs;

namespace EcommerceAPI.Business.Validators;

public class CheckoutRequestValidator : AbstractValidator<CheckoutRequest>
{
    public CheckoutRequestValidator()
    {
        RuleFor(x => x.ShippingAddress)
            .NotEmpty().WithMessage("Teslimat adresi zorunludur")
            .Length(10, 500).WithMessage("Teslimat adresi 10-500 karakter arasında olmalıdır");

        RuleFor(x => x.Notes)
            .MaximumLength(1000).WithMessage("Notlar 1000 karakteri geçemez");

        RuleFor(x => x.PaymentMethod)
            .NotEmpty().WithMessage("Ödeme yöntemi zorunludur");

        RuleFor(x => x.CouponCode)
            .MaximumLength(50);

        RuleFor(x => x.IdempotencyKey)
            .MaximumLength(100);

        RuleFor(x => x.LoyaltyPointsToUse)
            .GreaterThanOrEqualTo(0)
            .When(x => x.LoyaltyPointsToUse.HasValue)
            .WithMessage("Sadakat puanı negatif olamaz.");
    }
}
