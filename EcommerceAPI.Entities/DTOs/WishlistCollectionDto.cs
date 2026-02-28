using EcommerceAPI.Core.Entities;

namespace EcommerceAPI.Entities.DTOs;

public class WishlistCollectionDto : IDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
    public int ItemCount { get; set; }
}
