using System.Text.RegularExpressions;
using EcommerceAPI.Entities.DTOs;
using EcommerceAPI.Entities.Enums;
using FluentValidation;

namespace EcommerceAPI.Business.Validators;

public class ShipOrderRequestValidator : AbstractValidator<ShipOrderRequest>
{
    private static readonly Regex TrackingCodeRegex = new("^[A-Za-z0-9/-]{6,40}$", RegexOptions.Compiled);

    public ShipOrderRequestValidator()
    {
        RuleFor(x => x.CargoCompany)
            .MaximumLength(100).WithMessage("Kargo firması en fazla 100 karakter olabilir.");

        RuleFor(x => x.CargoProvider)
            .Must(provider => provider is null || provider != CargoProvider.Unknown)
            .WithMessage("Geçerli bir kargo firması seçmelisiniz.");

        RuleFor(x => x)
            .Must(x => x.CargoProvider.HasValue && x.CargoProvider.Value != CargoProvider.Unknown || !string.IsNullOrWhiteSpace(x.CargoCompany))
            .WithMessage("Kargo firması zorunludur.");

        RuleFor(x => x.TrackingCode)
            .NotEmpty().WithMessage("Takip kodu zorunludur.")
            .Must(BeValidTrackingCode)
            .WithMessage("Takip kodu 6-40 karakter olmalı ve yalnızca harf, rakam, tire veya eğik çizgi içermelidir.");

        RuleFor(x => x.EstimatedDeliveryDate)
            .Must(date => !date.HasValue || date.Value.Date >= DateTime.UtcNow.Date)
            .WithMessage("Tahmini teslimat tarihi bugunden once olamaz.");
    }

    private static bool BeValidTrackingCode(string trackingCode)
    {
        return !string.IsNullOrWhiteSpace(trackingCode) && TrackingCodeRegex.IsMatch(trackingCode.Trim());
    }
}
