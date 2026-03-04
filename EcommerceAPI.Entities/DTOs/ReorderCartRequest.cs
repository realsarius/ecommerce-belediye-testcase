using EcommerceAPI.Core.Entities;

namespace EcommerceAPI.Entities.DTOs;

public class ReorderCartRequest : IDto
{
    public int OrderId { get; set; }
}
