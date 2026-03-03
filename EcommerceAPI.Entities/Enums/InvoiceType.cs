using System.Text.Json.Serialization;

namespace EcommerceAPI.Entities.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum InvoiceType
{
    Individual = 1,
    Corporate = 2
}
