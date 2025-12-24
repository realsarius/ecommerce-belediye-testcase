namespace EcommerceAPI.Entities.Concrete;

public class Category : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    
    // Navigation properties
    public ICollection<Product> Products { get; set; } = new List<Product>();
}
