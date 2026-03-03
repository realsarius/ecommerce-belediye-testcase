namespace EcommerceAPI.Entities.Concrete;

public class ProductVariant : BaseEntity
{
    public int ProductId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public int SortOrder { get; set; }

    public Product Product { get; set; } = null!;
}
