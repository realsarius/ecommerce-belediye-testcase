using System.Text.Json.Serialization;

namespace EcommerceAPI.Entities.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CardBrand
{
    Unknown = 0,
    Visa = 1,
    Mastercard = 2,
    Troy = 3,
    Amex = 4
}
