using EcommerceAPI.Business.Abstract;
using EcommerceAPI.Business.Constants;
using EcommerceAPI.Core.Interfaces;
using EcommerceAPI.Core.Utilities.Results;
using EcommerceAPI.DataAccess.Abstract;
using EcommerceAPI.Entities.Concrete;
using EcommerceAPI.Entities.DTOs;
using EcommerceAPI.Entities.Enums;

namespace EcommerceAPI.Business.Concrete;

public class ProductReviewManager : IProductReviewService
{
    private readonly IProductReviewDal _reviewDal;
    private readonly IOrderDal _orderDal;
    private readonly IProductDal _productDal;
    private readonly ISellerProfileDal _sellerProfileDal;
    private readonly IUnitOfWork _unitOfWork;

    public ProductReviewManager(
        IProductReviewDal reviewDal,
        IOrderDal orderDal,
        IProductDal productDal,
        ISellerProfileDal sellerProfileDal,
        IUnitOfWork unitOfWork)
    {
        _reviewDal = reviewDal;
        _orderDal = orderDal;
        _productDal = productDal;
        _sellerProfileDal = sellerProfileDal;
        _unitOfWork = unitOfWork;
    }

    public async Task<IDataResult<ProductReviewDto>> CreateAsync(
        int userId, int productId, CreateReviewRequest request)
    {
        // 1. Ürün var mı?
        var product = await _productDal.GetAsync(p => p.Id == productId);
        if (product == null)
            return new ErrorDataResult<ProductReviewDto>(Messages.ReviewProductNotFound);

        // 2. Kullanıcı bu ürünü satın almış mı?
        var hasPurchased = await _orderDal.ExistsAsync(o => 
            o.UserId == userId && 
            o.OrderItems.Any(i => i.ProductId == productId) &&
            o.Status == OrderStatus.Delivered);

        if (!hasPurchased)
            return new ErrorDataResult<ProductReviewDto>(Messages.ReviewNotPurchased);

        // 3. Zaten yorum yapmış mı?
        var existing = await _reviewDal.GetByUserAndProductAsync(userId, productId);
        if (existing != null)
            return new ErrorDataResult<ProductReviewDto>(Messages.ReviewAlreadyExists);

        // 4. Oluştur
        var review = new ProductReview
        {
            ProductId = productId,
            UserId = userId,
            Rating = request.Rating,
            Comment = request.Comment.Trim(),
            ModerationStatus = ProductReviewModerationStatus.Pending
        };

        await _reviewDal.AddAsync(review);
        await _unitOfWork.SaveChangesAsync();

        // Ortalama puanı ve yorum sayısını güncelle
        await UpdateProductRatingStats(productId);

        var saved = await _reviewDal.GetByUserAndProductAsync(userId, productId);
        return new SuccessDataResult<ProductReviewDto>(MapToDto(saved!));
    }

    public async Task<IDataResult<ProductReviewDto>> UpdateAsync(
        int userId, int reviewId, UpdateReviewRequest request)
    {
        var review = await _reviewDal.GetByIdWithDetailsAsync(reviewId);
        if (review == null)
            return new ErrorDataResult<ProductReviewDto>(Messages.ReviewNotFound);

        if (review.UserId != userId)
            return new ErrorDataResult<ProductReviewDto>(Messages.ReviewUnauthorized);

        review.Rating = request.Rating;
        review.Comment = request.Comment.Trim();
        review.ModerationStatus = ProductReviewModerationStatus.Pending;
        review.ModerationNote = null;
        review.ModeratedAt = null;
        review.ModeratedByUserId = null;
        review.UpdatedAt = DateTime.UtcNow;

        _reviewDal.Update(review);
        await _unitOfWork.SaveChangesAsync();

        // Ortalama puanı ve yorum sayısını güncelle
        await UpdateProductRatingStats(review.ProductId);

        return new SuccessDataResult<ProductReviewDto>(MapToDto(review));
    }

    public async Task<IDataResult<ProductReviewDto>> SellerReplyAsync(
        int sellerUserId,
        int reviewId,
        SellerReviewReplyRequest request)
    {
        var sellerProfile = await _sellerProfileDal.GetByUserIdWithDetailsAsync(sellerUserId);
        if (sellerProfile == null)
            return new ErrorDataResult<ProductReviewDto>("Seller profili bulunamadı.");

        var review = await _reviewDal.GetByIdWithDetailsAsync(reviewId);
        if (review == null)
            return new ErrorDataResult<ProductReviewDto>(Messages.ReviewNotFound);

        if (review.Product?.SellerId != sellerProfile.Id)
            return new ErrorDataResult<ProductReviewDto>("Bu yoruma yanit verme yetkiniz yok.");

        if (review.ModerationStatus != ProductReviewModerationStatus.Approved)
            return new ErrorDataResult<ProductReviewDto>("Sadece onaylanan yorumlara yanit verilebilir.");

        var replyText = request.ReplyText?.Trim();
        if (string.IsNullOrWhiteSpace(replyText))
            return new ErrorDataResult<ProductReviewDto>("Yanit metni zorunludur.");

        review.SellerReply = replyText;
        review.SellerRepliedByUserId = sellerUserId;
        review.SellerRepliedAt = DateTime.UtcNow;
        review.UpdatedAt = DateTime.UtcNow;

        _reviewDal.Update(review);
        await _unitOfWork.SaveChangesAsync();

        return new SuccessDataResult<ProductReviewDto>(MapToDto(review), "Yorum yaniti kaydedildi.");
    }

    public async Task<IResult> DeleteAsync(int userId, int reviewId)
    {
        var review = await _reviewDal.GetByIdWithDetailsAsync(reviewId);
        if (review == null)
            return new ErrorResult(Messages.ReviewNotFound);

        if (review.UserId != userId)
            return new ErrorResult(Messages.ReviewUnauthorized);

        _reviewDal.Delete(review);
        await _unitOfWork.SaveChangesAsync();

        // Ortalama puanı ve yorum sayısını güncelle
        await UpdateProductRatingStats(review.ProductId);

        return new SuccessResult(Messages.ReviewDeleted);
    }

    public async Task<IResult> AdminDeleteAsync(int reviewId)
    {
        var review = await _reviewDal.GetByIdWithDetailsAsync(reviewId);
        if (review == null)
            return new ErrorResult(Messages.ReviewNotFound);

        _reviewDal.Delete(review);
        await _unitOfWork.SaveChangesAsync();

        // Ortalama puanı ve yorum sayısını güncelle
        await UpdateProductRatingStats(review.ProductId);

        return new SuccessResult(Messages.ReviewDeletedByAdmin);
    }

    public async Task<IDataResult<List<ProductReviewDto>>> GetByProductIdAsync(int productId)
    {
        var reviews = await _reviewDal.GetByProductIdAsync(productId, ProductReviewModerationStatus.Approved);
        return new SuccessDataResult<List<ProductReviewDto>>(
            reviews.Select(MapToDto).ToList());
    }

    public async Task<IDataResult<ProductReviewSummaryDto>> GetProductReviewSummaryAsync(int productId)
    {
        var reviews = await _reviewDal.GetByProductIdAsync(productId, ProductReviewModerationStatus.Approved);
        var summary = new ProductReviewSummaryDto
        {
            AverageRating = reviews.Count > 0 ? reviews.Average(r => r.Rating) : 0,
            TotalReviews = reviews.Count,
            RatingDistribution = Enumerable.Range(1, 5)
                .ToDictionary(star => star, star => reviews.Count(r => r.Rating == star))
        };
        return new SuccessDataResult<ProductReviewSummaryDto>(summary);
    }

    public async Task<IDataResult<List<ProductReviewDto>>> GetAdminReviewsAsync(ProductReviewModerationStatus? moderationStatus = null)
    {
        var reviews = await _reviewDal.GetAdminListAsync(moderationStatus);
        return new SuccessDataResult<List<ProductReviewDto>>(reviews.Select(MapToDto).ToList());
    }

    public async Task<IDataResult<List<ProductReviewDto>>> GetSellerReviewsAsync(
        int sellerUserId,
        int? productId = null,
        int? rating = null,
        bool? replied = null)
    {
        var sellerProfile = await _sellerProfileDal.GetByUserIdWithDetailsAsync(sellerUserId);
        if (sellerProfile == null)
            return new ErrorDataResult<List<ProductReviewDto>>("Seller profili bulunamadı.");

        if (rating.HasValue && (rating.Value < 1 || rating.Value > 5))
            return new ErrorDataResult<List<ProductReviewDto>>(Messages.ReviewRatingInvalid);

        var reviews = await _reviewDal.GetSellerListAsync(sellerProfile.Id, productId, rating, replied);
        return new SuccessDataResult<List<ProductReviewDto>>(reviews.Select(MapToDto).ToList());
    }

    public async Task<IDataResult<ProductReviewDto>> AdminApproveAsync(int reviewId, int adminUserId)
    {
        var review = await _reviewDal.GetByIdWithDetailsAsync(reviewId);
        if (review == null)
            return new ErrorDataResult<ProductReviewDto>(Messages.ReviewNotFound);

        review.ModerationStatus = ProductReviewModerationStatus.Approved;
        review.ModerationNote = null;
        review.ModeratedAt = DateTime.UtcNow;
        review.ModeratedByUserId = adminUserId;
        review.UpdatedAt = DateTime.UtcNow;

        _reviewDal.Update(review);
        await _unitOfWork.SaveChangesAsync();

        return new SuccessDataResult<ProductReviewDto>(MapToDto(review), "Yorum onaylandı.");
    }

    public async Task<IDataResult<ProductReviewDto>> AdminRejectAsync(int reviewId, int adminUserId, ReviewModerationRequest request)
    {
        var review = await _reviewDal.GetByIdWithDetailsAsync(reviewId);
        if (review == null)
            return new ErrorDataResult<ProductReviewDto>(Messages.ReviewNotFound);

        review.ModerationStatus = ProductReviewModerationStatus.Rejected;
        review.ModerationNote = request.ModerationNote?.Trim();
        review.ModeratedAt = DateTime.UtcNow;
        review.ModeratedByUserId = adminUserId;
        review.UpdatedAt = DateTime.UtcNow;

        _reviewDal.Update(review);
        await _unitOfWork.SaveChangesAsync();

        return new SuccessDataResult<ProductReviewDto>(MapToDto(review), "Yorum reddedildi.");
    }

    public async Task<IResult> AdminBulkApproveAsync(IEnumerable<int> reviewIds, int adminUserId)
    {
        var ids = reviewIds
            .Where(id => id > 0)
            .Distinct()
            .ToList();

        if (ids.Count == 0)
            return new ErrorResult("Onaylanacak yorum bulunamadı.");

        var reviews = await _reviewDal.GetAdminListAsync();
        var targetReviews = reviews.Where(review => ids.Contains(review.Id)).ToList();

        if (targetReviews.Count == 0)
            return new ErrorResult(Messages.ReviewNotFound);

        foreach (var review in targetReviews)
        {
            review.ModerationStatus = ProductReviewModerationStatus.Approved;
            review.ModerationNote = null;
            review.ModeratedAt = DateTime.UtcNow;
            review.ModeratedByUserId = adminUserId;
            review.UpdatedAt = DateTime.UtcNow;
            _reviewDal.Update(review);
        }

        await _unitOfWork.SaveChangesAsync();
        return new SuccessResult("Secilen yorumlar onaylandi.");
    }
    
    private async Task UpdateProductRatingStats(int productId)
    {
        var product = await _productDal.GetAsync(p => p.Id == productId);
        if (product != null)
        {
            var (average, count) = await _reviewDal.GetProductRatingAsync(productId);
            return; // Db'ye product eklemedik ki henüz db seviyesinde ayrı sütunda durmuyor DTO seviyesinde veriyoruz.
        }
    }

    public async Task<IDataResult<bool>> CanUserReviewAsync(int userId, int productId)
    {
        // 1. Zaten yorum yaptıysa tekrar yapamaz
        var existing = await _reviewDal.GetByUserAndProductAsync(userId, productId);
        if (existing != null)
            return new SuccessDataResult<bool>(false);

        // 2. Satın almış ve tamamlanmış (Delivery vs) siparişi var mı?
        var hasPurchased = await _orderDal.ExistsAsync(o => 
            o.UserId == userId && 
            o.OrderItems.Any(i => i.ProductId == productId) &&
            o.Status == OrderStatus.Delivered);

        return new SuccessDataResult<bool>(hasPurchased);
    }

    private static ProductReviewDto MapToDto(ProductReview review) => new()
    {
        Id = review.Id,
        ProductId = review.ProductId,
        ProductName = review.Product?.Name ?? string.Empty,
        UserId = review.UserId,
        UserFullName = review.User != null
            ? $"{review.User.FirstName} {review.User.LastName}".Trim()
            : "Anonim",
        Rating = review.Rating,
        Comment = review.Comment,
        SellerReply = review.SellerReply,
        SellerRepliedAt = review.SellerRepliedAt,
        SellerRepliedByUserId = review.SellerRepliedByUserId,
        ModerationStatus = review.ModerationStatus.ToString(),
        ModerationNote = review.ModerationNote,
        ModeratedAt = review.ModeratedAt,
        ModeratedByUserId = review.ModeratedByUserId,
        CreatedAt = review.CreatedAt,
        UpdatedAt = review.UpdatedAt,
    };
}
