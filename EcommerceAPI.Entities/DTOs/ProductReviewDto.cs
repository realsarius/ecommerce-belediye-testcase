using EcommerceAPI.Core.Entities;

namespace EcommerceAPI.Entities.DTOs;

public class ProductReviewDto : IDto
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int UserId { get; set; }
    public string UserFullName { get; set; } = string.Empty;
    public int Rating { get; set; }
    public string Comment { get; set; } = string.Empty;
    public string? SellerReply { get; set; }
    public int? SellerRepliedByUserId { get; set; }
    public DateTime? SellerRepliedAt { get; set; }
    public string ModerationStatus { get; set; } = string.Empty;
    public string? ModerationNote { get; set; }
    public int? ModeratedByUserId { get; set; }
    public DateTime? ModeratedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class CreateReviewRequest
{
    public int Rating { get; set; }
    public string Comment { get; set; } = string.Empty;
}

public class UpdateReviewRequest
{
    public int Rating { get; set; }
    public string Comment { get; set; } = string.Empty;
}

public class ReviewModerationRequest : IDto
{
    public string? ModerationNote { get; set; }
}

public class SellerReviewReplyRequest : IDto
{
    public string ReplyText { get; set; } = string.Empty;
}

public class BulkApproveReviewsRequest : IDto
{
    public List<int> Ids { get; set; } = new();
}

public class ProductReviewSummaryDto
{
    public double AverageRating { get; set; }
    public int TotalReviews { get; set; }
    public Dictionary<int, int> RatingDistribution { get; set; } = new();
}
