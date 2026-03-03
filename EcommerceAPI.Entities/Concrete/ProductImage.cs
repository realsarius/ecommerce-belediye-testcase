namespace EcommerceAPI.Entities.Concrete;

public class ProductImage : BaseEntity
{
    public int ProductId { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public bool IsPrimary { get; set; }

    public Product Product { get; set; } = null!;
}
