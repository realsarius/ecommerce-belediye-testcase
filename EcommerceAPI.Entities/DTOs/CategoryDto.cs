using EcommerceAPI.Core.Entities;
namespace EcommerceAPI.Entities.DTOs;

public class CategoryDto : IDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public int? ParentCategoryId { get; set; }
    public int SortOrder { get; set; }
    public int ProductCount { get; set; }
    public int ChildCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class ReorderCategoryItemRequest : IDto
{
    public int Id { get; set; }
    public int? ParentCategoryId { get; set; }
    public int SortOrder { get; set; }
}

public class ReorderCategoriesRequest : IDto
{
    public List<ReorderCategoryItemRequest> Items { get; set; } = new();
}
