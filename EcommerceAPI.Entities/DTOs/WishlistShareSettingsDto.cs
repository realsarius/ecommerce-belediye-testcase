using EcommerceAPI.Core.Entities;

namespace EcommerceAPI.Entities.DTOs;

public class WishlistShareSettingsDto : IDto
{
    public bool IsPublic { get; set; }
    public Guid? ShareToken { get; set; }
    public string? SharePath { get; set; }
}
