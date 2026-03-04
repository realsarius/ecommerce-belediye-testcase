using System.Text.Json.Serialization;

namespace EcommerceAPI.Entities.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum PaymentProviderType
{
    Iyzico = 0,
    Stripe = 1,
    PayTR = 2
}
