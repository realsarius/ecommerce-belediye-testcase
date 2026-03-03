using EcommerceAPI.Core.Entities;

namespace EcommerceAPI.Entities.DTOs;

public class BulkUpdateProductsRequest : IDto
{
    public List<int> Ids { get; set; } = new();
    public string Action { get; set; } = string.Empty;
}
