using System.Collections.Generic;
using EcommerceAPI.Core.Entities;

namespace EcommerceAPI.Entities.DTOs;

public class CursorPaginatedResponse<T> : IDto
{
    public int Limit { get; set; }
    public bool HasMore { get; set; }
    public string? NextCursor { get; set; }
    public List<T> Items { get; set; } = new();
}
