using EcommerceAPI.Core.Entities;
using System.ComponentModel.DataAnnotations;

namespace EcommerceAPI.Entities.DTOs;

public class UpdateProductRequest : IDto
{
    [Required(ErrorMessage = "Ürün adı zorunludur")]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string Description { get; set; } = string.Empty;

    [Required(ErrorMessage = "Fiyat zorunludur")]
    [Range(0.01, double.MaxValue, ErrorMessage = "Fiyat 0'dan büyük olmalıdır")]
    public decimal Price { get; set; }

    [Required(ErrorMessage = "Kategori zorunludur")]
    public int CategoryId { get; set; }

    public bool IsActive { get; set; } = true;
}
