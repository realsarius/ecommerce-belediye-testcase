using EcommerceAPI.Core.Entities;

namespace EcommerceAPI.Entities.DTOs;

public class CreateCategoryRequest : IDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public int? ParentCategoryId { get; set; }
}
