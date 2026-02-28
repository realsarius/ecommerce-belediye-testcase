using EcommerceAPI.Core.Entities;

namespace EcommerceAPI.Entities.DTOs;

public class WishlistPriceAlertDto : IDto
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string Currency { get; set; } = "TRY";
    public decimal CurrentPrice { get; set; }
    public decimal TargetPrice { get; set; }
    public bool IsActive { get; set; }
    public decimal? LastTriggeredPrice { get; set; }
    public DateTime? LastNotifiedAt { get; set; }
    public DateTime CreatedAt { get; set; }
}
