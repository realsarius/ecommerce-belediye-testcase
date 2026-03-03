using EcommerceAPI.Entities.Enums;
using EcommerceAPI.Entities.Utilities;
using FluentAssertions;

namespace EcommerceAPI.UnitTests;

public class CardBrandDetectorTests
{
    [Theory]
    [InlineData("4111 1111 1111 1111", CardBrand.Visa)]
    [InlineData("5555 5555 5555 4444", CardBrand.Mastercard)]
    [InlineData("2221 0000 0000 0009", CardBrand.Mastercard)]
    [InlineData("9792 0303 9444 0796", CardBrand.Troy)]
    [InlineData("3782 822463 10005", CardBrand.Amex)]
    [InlineData("1234 5678 9012 3456", CardBrand.Unknown)]
    public void Detect_ShouldResolveKnownBrands(string cardNumber, CardBrand expectedBrand)
    {
        var result = CardBrandDetector.Detect(cardNumber);

        result.Should().Be(expectedBrand);
    }
}
