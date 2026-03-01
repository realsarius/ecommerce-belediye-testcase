using EcommerceAPI.Core.Entities;

namespace EcommerceAPI.Entities.DTOs;

public class CreateReturnRequestRequest : IDto
{
    public string Type { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public string? RequestNote { get; set; }
}
