using System.ComponentModel.DataAnnotations;

namespace EcommerceAPI.Core.DTOs;

public class UpdateCartItemRequest
{
    [Required(ErrorMessage = "Miktar zorunludur")]
    [Range(1, 100, ErrorMessage = "Miktar 1 ile 100 arasında olmalıdır")]
    public int Quantity { get; set; }
}
