using System.ComponentModel.DataAnnotations;

namespace EcommerceAPI.Entities.DTOs;

public class AddWishlistItemRequest
{
    [Range(1, int.MaxValue, ErrorMessage = "Geçerli bir ürün seçmelisiniz.")]
    public int ProductId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Geçerli bir koleksiyon seçmelisiniz.")]
    public int? CollectionId { get; set; }
}
