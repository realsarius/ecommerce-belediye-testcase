using EcommerceAPI.Core.Entities;

namespace EcommerceAPI.Entities.DTOs;

public class ProductVariantDto : IDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public int SortOrder { get; set; }
}
