using System.ComponentModel.DataAnnotations;

namespace EcommerceAPI.Core.DTOs;

public class CreateCategoryRequest
{
    [Required(ErrorMessage = "Kategori adı zorunludur")]
    [StringLength(255, MinimumLength = 2, ErrorMessage = "Kategori adı 2-255 karakter arasında olmalıdır")]
    public string Name { get; set; } = string.Empty;
    
    [StringLength(1000, ErrorMessage = "Açıklama en fazla 1000 karakter olabilir")]
    public string? Description { get; set; }
}
