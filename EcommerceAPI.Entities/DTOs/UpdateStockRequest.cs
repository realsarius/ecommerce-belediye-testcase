using EcommerceAPI.Core.Entities;
using System.ComponentModel.DataAnnotations;

namespace EcommerceAPI.Entities.DTOs;

public class UpdateStockRequest : IDto
{
    [Required(ErrorMessage = "Miktar zorunludur")]
    public int Delta { get; set; }  // +10 stok girişi, -5 stok çıkışı

    [Required(ErrorMessage = "Açıklama zorunludur")]
    [MaxLength(500)]
    public string Reason { get; set; } = string.Empty;  // "Stok Girişi", "Satış", "Düzeltme"

    [MaxLength(1000)]
    public string Notes { get; set; } = string.Empty;
}
