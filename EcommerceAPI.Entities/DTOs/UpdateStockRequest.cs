using EcommerceAPI.Core.Entities;

namespace EcommerceAPI.Entities.DTOs;

public class UpdateStockRequest : IDto
{
    public int Delta { get; set; }  // +10 stok girişi, -5 stok çıkışı
    public string Reason { get; set; } = string.Empty;  // "Stok Girişi", "Satış", "Düzeltme"
    public string Notes { get; set; } = string.Empty;
}
