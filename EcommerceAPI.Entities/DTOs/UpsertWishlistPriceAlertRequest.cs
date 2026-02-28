using EcommerceAPI.Core.Entities;

namespace EcommerceAPI.Entities.DTOs;

public class UpsertWishlistPriceAlertRequest : IDto
{
    public int ProductId { get; set; }
    public decimal TargetPrice { get; set; }
}
