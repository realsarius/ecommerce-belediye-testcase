namespace EcommerceAPI.Entities.Concrete;

public class OrderItem : BaseEntity
{
    public int OrderId { get; set; }
    public int ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal PriceSnapshot { get; set; }
    
    public Order Order { get; set; } = null!;
    public Product Product { get; set; } = null!;
}
