using EcommerceAPI.Core.Entities;

namespace EcommerceAPI.Entities.DTOs;

/// <summary>
/// Request model for updating order items (add/remove/change quantity).
/// Only applicable for orders in PendingPayment status.
/// </summary>
public class UpdateOrderItemsRequest : IDto
{
    public List<UpdateOrderItemDto> Items { get; set; } = new();
}

public class UpdateOrderItemDto : IDto
{
    public int ProductId { get; set; }
    public int Quantity { get; set; }
}
