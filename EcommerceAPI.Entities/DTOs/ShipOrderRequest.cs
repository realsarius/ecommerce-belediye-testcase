using EcommerceAPI.Entities.Enums;

namespace EcommerceAPI.Entities.DTOs;

public class ShipOrderRequest
{
    public string TrackingCode { get; set; } = string.Empty;
    public CargoProvider? CargoProvider { get; set; }
    public string? CargoCompany { get; set; }
    public DateTime? EstimatedDeliveryDate { get; set; }
}
