using EcommerceAPI.Entities.DTOs;
using FluentValidation;

namespace EcommerceAPI.Business.Validators;

public class AddCreditCardRequestValidator : AbstractValidator<AddCreditCardRequest>
{
    public AddCreditCardRequestValidator()
    {
        RuleFor(x => x.CardAlias)
            .NotEmpty().WithMessage("Kart takma adı zorunludur")
            .MaximumLength(100).WithMessage("Kart takma adı en fazla 100 karakter olabilir");

        RuleFor(x => x.CardHolderName)
            .NotEmpty().WithMessage("Kart üzerindeki isim zorunludur")
            .MaximumLength(200).WithMessage("Kart sahibi adı en fazla 200 karakter olabilir");

        RuleFor(x => x.CardNumber)
            .NotEmpty().WithMessage("Kart numarası zorunludur")
            .Must(BeValidCardNumber).WithMessage("Geçersiz kart numarası. 16 haneli sayısal değer giriniz");

        RuleFor(x => x.ExpireMonth)
            .NotEmpty().WithMessage("Son kullanma ayı zorunludur")
            .Must(BeValidMonth).WithMessage("Son kullanma ayı 01-12 arasında olmalıdır");

        RuleFor(x => x.ExpireYear)
            .NotEmpty().WithMessage("Son kullanma yılı zorunludur")
            .Must(BeValidYear).WithMessage("Son kullanma yılı geçerli bir yıl olmalıdır");

        RuleFor(x => x)
            .Must(NotBeExpired).WithMessage("Kart son kullanma tarihi geçmiş olamaz");

        RuleFor(x => x.Cvv)
            .NotEmpty().WithMessage("Güvenlik kodu (CVV) zorunludur")
            .Must(BeValidCvv).WithMessage("CVV 3 veya 4 haneli sayısal değer olmalıdır");
    }

    private bool BeValidCardNumber(string cardNumber)
    {
        if (string.IsNullOrWhiteSpace(cardNumber))
            return false;

        string digitsOnly = new string(cardNumber.Where(char.IsDigit).ToArray());
        
        if (digitsOnly.Length < 13 || digitsOnly.Length > 19)
            return false;

        return IsValidLuhn(digitsOnly);
    }

    private bool IsValidLuhn(string number)
    {
        int sum = 0;
        bool alternate = false;

        for (int i = number.Length - 1; i >= 0; i--)
        {
            int digit = number[i] - '0';

            if (alternate)
            {
                digit *= 2;
                if (digit > 9)
                    digit -= 9;
            }

            sum += digit;
            alternate = !alternate;
        }

        return sum % 10 == 0;
    }

    private bool BeValidMonth(string month)
    {
        if (string.IsNullOrWhiteSpace(month))
            return false;

        if (!int.TryParse(month, out int monthValue))
            return false;

        return monthValue >= 1 && monthValue <= 12;
    }

    private bool BeValidYear(string year)
    {
        if (string.IsNullOrWhiteSpace(year))
            return false;

        if (!int.TryParse(year, out int yearValue))
            return false;

        int currentYear = DateTime.UtcNow.Year;
        
        if (year.Length == 2)
        {
            yearValue = 2000 + yearValue;
        }
        
        return yearValue >= currentYear && yearValue <= currentYear + 20;
    }

    private bool NotBeExpired(AddCreditCardRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.ExpireMonth) || string.IsNullOrWhiteSpace(request.ExpireYear))
            return true;

        if (!int.TryParse(request.ExpireMonth, out int month) || !int.TryParse(request.ExpireYear, out int year))
            return true;

        if (request.ExpireYear.Length == 2)
        {
            year = 2000 + year;
        }

        var now = DateTime.UtcNow;
        var expiryDate = new DateTime(year, month, 1).AddMonths(1).AddDays(-1);
        
        return expiryDate >= now;
    }

    private bool BeValidCvv(string cvv)
    {
        if (string.IsNullOrWhiteSpace(cvv))
            return false;

        string digitsOnly = new string(cvv.Where(char.IsDigit).ToArray());
        
        return digitsOnly.Length >= 3 && digitsOnly.Length <= 4;
    }
}
