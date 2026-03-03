using System.Collections.Generic;
using EcommerceAPI.Core.Entities;

namespace EcommerceAPI.Entities.DTOs;

public class SharedWishlistDto : CursorPaginatedResponse<WishlistItemDto>
{
    public int Id { get; set; }
    public string OwnerDisplayName { get; set; } = string.Empty;
}
