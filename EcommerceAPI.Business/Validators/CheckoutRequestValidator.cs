using FluentValidation;
using EcommerceAPI.Entities.DTOs;
using EcommerceAPI.Entities.Enums;

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

        RuleFor(x => x.GiftCardCode)
            .MaximumLength(64);

        RuleFor(x => x.PreliminaryInfoAccepted)
            .Equal(true)
            .WithMessage("Ön bilgilendirme formunu onaylamalısınız");

        RuleFor(x => x.DistanceSalesContractAccepted)
            .Equal(true)
            .WithMessage("Mesafeli satış sözleşmesini onaylamalısınız");

        RuleFor(x => x.InvoiceInfo)
            .NotNull()
            .WithMessage("Fatura bilgisi zorunludur");

        When(x => x.InvoiceInfo != null, () =>
        {
            RuleFor(x => x.InvoiceInfo!.InvoiceAddress)
                .NotEmpty().WithMessage("Fatura adresi zorunludur")
                .Length(10, 2000).WithMessage("Fatura adresi 10-2000 karakter arasında olmalıdır");

            RuleFor(x => x.InvoiceInfo!.FullName)
                .NotEmpty().WithMessage("Ad soyad zorunludur")
                .MaximumLength(200)
                .When(x => x.InvoiceInfo!.Type == InvoiceType.Individual);

            RuleFor(x => x.InvoiceInfo!.TcKimlikNo)
                .Matches(@"^\d{11}$")
                .When(x => x.InvoiceInfo!.Type == InvoiceType.Individual && !string.IsNullOrWhiteSpace(x.InvoiceInfo!.TcKimlikNo))
                .WithMessage("TC kimlik numarası 11 haneli olmalıdır");

            RuleFor(x => x.InvoiceInfo!.CompanyName)
                .NotEmpty().WithMessage("Şirket adı zorunludur")
                .MaximumLength(200)
                .When(x => x.InvoiceInfo!.Type == InvoiceType.Corporate);

            RuleFor(x => x.InvoiceInfo!.TaxOffice)
                .NotEmpty().WithMessage("Vergi dairesi zorunludur")
                .MaximumLength(200)
                .When(x => x.InvoiceInfo!.Type == InvoiceType.Corporate);

            RuleFor(x => x.InvoiceInfo!.TaxNumber)
                .NotEmpty().WithMessage("Vergi numarası zorunludur")
                .Matches(@"^\d{10}$")
                .When(x => x.InvoiceInfo!.Type == InvoiceType.Corporate)
                .WithMessage("Vergi numarası 10 haneli olmalıdır");
        });
    }
}
