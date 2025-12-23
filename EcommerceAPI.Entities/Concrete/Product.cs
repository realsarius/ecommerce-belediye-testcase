namespace EcommerceAPI.Entities.Concrete;

public class Product : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Currency { get; set; } = "TRY";
    public string SKU { get; set; } = string.Empty; 
    public bool IsActive { get; set; } = true;
    public int CategoryId { get; set; }
    
    // Navigation properties
    public Category Category { get; set; } = null!;
    public Inventory? Inventory { get; set; }
    public ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();
    public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    public ICollection<InventoryMovement> InventoryMovements { get; set; } = new List<InventoryMovement>();
}
