namespace EcommerceAPI.Entities.Concrete;

public class InventoryMovement : BaseEntity
{
    public int ProductId { get; set; }
    public int UserId { get; set; }
    public int Delta { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    

    public Product Product { get; set; } = null!;
    public User User { get; set; } = null!;
}
