using FluentValidation;
using EcommerceAPI.Entities.DTOs;

namespace EcommerceAPI.Business.Validators;

public class ProcessPaymentRequestValidator : AbstractValidator<ProcessPaymentRequest>
{
    public ProcessPaymentRequestValidator()
    {
        RuleFor(x => x.OrderId)
            .GreaterThan(0).WithMessage("Sipariş ID zorunludur");

        RuleFor(x => x.CVV)
            .Must(cvv =>
            {
                if (string.IsNullOrWhiteSpace(cvv))
                {
                    return true;
                }

                var digitsOnly = new string(cvv.Where(char.IsDigit).ToArray());
                return digitsOnly.Length is >= 3 and <= 4;
            })
            .WithMessage("CVV 3 veya 4 haneli sayisal deger olmalidir.");

        RuleFor(x => x.SaveCardAlias)
            .MaximumLength(100)
            .WithMessage("Kart takma adi en fazla 100 karakter olabilir.");
    }
}
