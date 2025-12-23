using EcommerceAPI.Core.Entities;
using System.ComponentModel.DataAnnotations;

namespace EcommerceAPI.Entities.DTOs;

public class UpdateCartItemRequest : IDto
{
    [Required(ErrorMessage = "Miktar zorunludur")]
    [Range(1, 100, ErrorMessage = "Miktar 1 ile 100 arasında olmalıdır")]
    public int Quantity { get; set; }
}
