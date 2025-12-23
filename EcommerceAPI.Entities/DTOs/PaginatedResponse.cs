using EcommerceAPI.Core.Entities;
namespace EcommerceAPI.Entities.DTOs;

public class PaginatedResponse<T> : IDto
{
    public List<T> Items { get; set; } = new();
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages => PageSize > 0 
        ? (int)Math.Ceiling(TotalCount / (double)PageSize) 
        : 0;
    public bool HasPreviousPage => Page > 1;
    public bool HasNextPage => Page < TotalPages;
}
