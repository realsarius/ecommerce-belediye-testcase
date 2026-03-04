namespace EcommerceAPI.Entities.Enums;

public enum ShipmentStatus
{
    Pending = 0,
    Preparing = 1,
    HandedToCargo = 2,
    InTransit = 3,
    OutForDelivery = 4,
    Delivered = 5,
    Failed = 6,
    Returned = 7
}
