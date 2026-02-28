namespace EcommerceAPI.Entities.Concrete;

public class PriceAlert : BaseEntity
{
    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;

    public decimal TargetPrice { get; set; }
    public bool IsActive { get; set; } = true;

    public decimal LastKnownPrice { get; set; }
    public decimal? LastTriggeredPrice { get; set; }
    public DateTime? LastNotifiedAt { get; set; }
}
