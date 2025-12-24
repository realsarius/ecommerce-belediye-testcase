using EcommerceAPI.Core.Entities;
namespace EcommerceAPI.Entities.DTOs;

public class ProductListRequest : IDto
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public int? CategoryId { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public string? Search { get; set; }
    public bool? InStock { get; set; }
    // default: sort by name
    public string SortBy { get; set; } = "name";
    public bool SortDescending { get; set; } = false;
}
