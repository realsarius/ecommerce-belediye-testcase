using System.ComponentModel.DataAnnotations;

namespace EcommerceAPI.Entities.DTOs;

public class CreateWishlistCollectionRequest
{
    [Required(ErrorMessage = "Koleksiyon adı zorunludur.")]
    [StringLength(80, MinimumLength = 2, ErrorMessage = "Koleksiyon adı 2 ile 80 karakter arasında olmalıdır.")]
    public string Name { get; set; } = string.Empty;
}
