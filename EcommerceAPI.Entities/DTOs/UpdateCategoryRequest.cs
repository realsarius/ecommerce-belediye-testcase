using EcommerceAPI.Core.Entities;

namespace EcommerceAPI.Entities.DTOs;

public class UpdateCategoryRequest : IDto
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public bool? IsActive { get; set; }
    public int? ParentCategoryId { get; set; }
    public int? SortOrder { get; set; }
}
