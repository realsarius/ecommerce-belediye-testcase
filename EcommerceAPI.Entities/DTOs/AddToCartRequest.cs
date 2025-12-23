using EcommerceAPI.Core.Entities;
using System.ComponentModel.DataAnnotations;

namespace EcommerceAPI.Entities.DTOs;

public class AddToCartRequest : IDto
{
    [Required(ErrorMessage = "Ürün ID zorunludur")]
    [Range(1, int.MaxValue, ErrorMessage = "Geçerli bir ürün ID giriniz")]
    public int ProductId { get; set; }

    [Required(ErrorMessage = "Miktar zorunludur")]
    [Range(1, 100, ErrorMessage = "Miktar 1 ile 100 arasında olmalıdır")]
    public int Quantity { get; set; } = 1;
}
