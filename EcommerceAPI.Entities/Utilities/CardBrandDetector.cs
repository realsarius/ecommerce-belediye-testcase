using System.Text.RegularExpressions;
using EcommerceAPI.Entities.Enums;

namespace EcommerceAPI.Entities.Utilities;

public static partial class CardBrandDetector
{
    public static CardBrand Detect(string? cardNumber)
    {
        if (string.IsNullOrWhiteSpace(cardNumber))
        {
            return CardBrand.Unknown;
        }

        var digits = new string(cardNumber.Where(char.IsDigit).ToArray());
        if (digits.Length < 4)
        {
            return CardBrand.Unknown;
        }

        if (AmexRegex().IsMatch(digits))
        {
            return CardBrand.Amex;
        }

        if (TroyRegex().IsMatch(digits))
        {
            return CardBrand.Troy;
        }

        if (MastercardRegex().IsMatch(digits))
        {
            return CardBrand.Mastercard;
        }

        if (VisaRegex().IsMatch(digits))
        {
            return CardBrand.Visa;
        }

        return CardBrand.Unknown;
    }

    [GeneratedRegex(@"^3[47][0-9]{13}$")]
    private static partial Regex AmexRegex();

    [GeneratedRegex(@"^(9792[0-9]{12}|65[0-9]{14}|2205[0-9]{12})$")]
    private static partial Regex TroyRegex();

    [GeneratedRegex(@"^(5[1-5][0-9]{14}|2(2(2[1-9]|[3-9][0-9])|[3-6][0-9]{2}|7([01][0-9]|20))[0-9]{12})$")]
    private static partial Regex MastercardRegex();

    [GeneratedRegex(@"^4[0-9]{12}([0-9]{3}){0,2}$")]
    private static partial Regex VisaRegex();
}
