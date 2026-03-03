using EcommerceAPI.Core.Entities;

namespace EcommerceAPI.Entities.DTOs;

public class CreateReturnRequestRequest : IDto
{
    public string Type { get; set; } = string.Empty;
    public string ReasonCategory { get; set; } = string.Empty;
    public List<int>? SelectedOrderItemIds { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string? RequestNote { get; set; }
}
