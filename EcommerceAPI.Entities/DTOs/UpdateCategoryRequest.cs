using EcommerceAPI.Core.Entities;
using System.ComponentModel.DataAnnotations;

namespace EcommerceAPI.Entities.DTOs;

public class UpdateCategoryRequest : IDto
{
    [StringLength(255, MinimumLength = 2, ErrorMessage = "Kategori adı 2-255 karakter arasında olmalıdır")]
    public string? Name { get; set; }
    
    [StringLength(1000, ErrorMessage = "Açıklama en fazla 1000 karakter olabilir")]
    public string? Description { get; set; }
    
    public bool? IsActive { get; set; }
}
