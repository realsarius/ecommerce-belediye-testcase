using EcommerceAPI.Core.Entities;

namespace EcommerceAPI.Entities.DTOs;

public class CreateCategoryRequest : IDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}
