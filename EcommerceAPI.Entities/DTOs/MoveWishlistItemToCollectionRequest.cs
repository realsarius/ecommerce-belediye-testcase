using System.ComponentModel.DataAnnotations;

namespace EcommerceAPI.Entities.DTOs;

public class MoveWishlistItemToCollectionRequest
{
    [Range(1, int.MaxValue, ErrorMessage = "Geçerli bir koleksiyon seçmelisiniz.")]
    public int CollectionId { get; set; }
}
