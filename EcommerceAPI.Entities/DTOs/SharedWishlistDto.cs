using System.Collections.Generic;
using EcommerceAPI.Core.Entities;

namespace EcommerceAPI.Entities.DTOs;

public class SharedWishlistDto : IDto
{
    public int Id { get; set; }
    public string OwnerDisplayName { get; set; } = string.Empty;
    public int Limit { get; set; }
    public bool HasMore { get; set; }
    public string? NextCursor { get; set; }
    public List<WishlistItemDto> Items { get; set; } = new();
}
