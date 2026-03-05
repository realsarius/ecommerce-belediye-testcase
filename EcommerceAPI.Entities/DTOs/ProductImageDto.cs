using EcommerceAPI.Core.Entities;

namespace EcommerceAPI.Entities.DTOs;

public class ProductImageDto : IDto
{
    public int Id { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public string? ObjectKey { get; set; }
    public int SortOrder { get; set; }
    public bool IsPrimary { get; set; }
}
