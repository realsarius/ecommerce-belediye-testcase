using EcommerceAPI.Entities.Enums;

namespace EcommerceAPI.Entities.Concrete;

public class ProductReview : BaseEntity
{
    public int ProductId { get; set; }
    public int UserId { get; set; }
    public int Rating { get; set; }
    public string Comment { get; set; } = string.Empty;
    public ProductReviewModerationStatus ModerationStatus { get; set; } = ProductReviewModerationStatus.Approved;
    public string? ModerationNote { get; set; }
    public int? ModeratedByUserId { get; set; }
    public DateTime? ModeratedAt { get; set; }

    // Navigation
    public Product Product { get; set; } = null!;
    public User User { get; set; } = null!;
}
