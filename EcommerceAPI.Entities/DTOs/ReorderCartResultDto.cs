using EcommerceAPI.Core.Entities;

namespace EcommerceAPI.Entities.DTOs;

public class ReorderCartResultDto : IDto
{
    public int RequestedCount { get; set; }
    public int AddedCount { get; set; }
    public int SkippedCount { get; set; }
    public List<ReorderCartSkippedProductDto> SkippedProducts { get; set; } = new();
}

public class ReorderCartSkippedProductDto : IDto
{
    public int ProductId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
}
