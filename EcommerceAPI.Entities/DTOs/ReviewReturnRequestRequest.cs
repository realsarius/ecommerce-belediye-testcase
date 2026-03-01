using EcommerceAPI.Core.Entities;

namespace EcommerceAPI.Entities.DTOs;

public class ReviewReturnRequestRequest : IDto
{
    public string Status { get; set; } = string.Empty;
    public string? ReviewNote { get; set; }
}
