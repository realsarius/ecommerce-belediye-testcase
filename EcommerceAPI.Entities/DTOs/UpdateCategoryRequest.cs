using EcommerceAPI.Core.Entities;

namespace EcommerceAPI.Entities.DTOs;

public class UpdateCategoryRequest : IDto
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public bool? IsActive { get; set; }
}
