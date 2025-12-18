using System.ComponentModel.DataAnnotations;

namespace EcommerceAPI.Core.DTOs;

public class CreateProductRequest
{
    [Required(ErrorMessage = "Ürün adı zorunludur")]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string Description { get; set; } = string.Empty;

    [Required(ErrorMessage = "Fiyat zorunludur")]
    [Range(0.01, double.MaxValue, ErrorMessage = "Fiyat 0'dan büyük olmalıdır")]
    public decimal Price { get; set; }

    [Required(ErrorMessage = "SKU zorunludur")]
    [MaxLength(50)]
    public string SKU { get; set; } = string.Empty;

    [Required(ErrorMessage = "Kategori zorunludur")]
    public int CategoryId { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "Stok negatif olamaz")]
    public int InitialStock { get; set; } = 0;
}
