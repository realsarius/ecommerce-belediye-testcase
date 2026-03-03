namespace EcommerceAPI.Entities.DTOs;

public class ShipOrderRequest
{
    public string TrackingCode { get; set; } = string.Empty;
    public string CargoCompany { get; set; } = string.Empty;
}
