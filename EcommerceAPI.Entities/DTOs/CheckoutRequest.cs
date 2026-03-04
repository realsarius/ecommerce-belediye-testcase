using EcommerceAPI.Core.Entities;
using System.Text.Json.Serialization;

namespace EcommerceAPI.Entities.DTOs;

public class CheckoutRequest : IDto
{
    public string ShippingAddress { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
    public string? CouponCode { get; set; }
    public string? IdempotencyKey { get; set; }
    public int? LoyaltyPointsToUse { get; set; }
    public string? GiftCardCode { get; set; }
    public bool PreliminaryInfoAccepted { get; set; }
    public bool DistanceSalesContractAccepted { get; set; }
    public CheckoutInvoiceInfoRequest? InvoiceInfo { get; set; }

    [JsonIgnore]
    public string? AcceptedFromIp { get; set; }
}
