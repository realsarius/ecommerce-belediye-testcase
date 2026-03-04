using System.Text.Json.Serialization;

namespace EcommerceAPI.Entities.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CargoProvider
{
    Unknown = 0,
    YurticiKargo = 1,
    ArasCargo = 2,
    MngKargo = 3,
    PttKargo = 4,
    SuratKargo = 5,
    UpsKargo = 6
}
