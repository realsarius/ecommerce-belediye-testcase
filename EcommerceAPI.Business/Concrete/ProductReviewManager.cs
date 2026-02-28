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
    private readonly IUnitOfWork _unitOfWork;

    public ProductReviewManager(
        IProductReviewDal reviewDal,
        IOrderDal orderDal,
        IProductDal productDal,
        IUnitOfWork unitOfWork)
    {
        _reviewDal = reviewDal;
        _orderDal = orderDal;
        _productDal = productDal;
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
            Comment = request.Comment.Trim()
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
        var review = await _reviewDal.GetAsync(r => r.Id == reviewId);
        if (review == null)
            return new ErrorDataResult<ProductReviewDto>(Messages.ReviewNotFound);

        if (review.UserId != userId)
            return new ErrorDataResult<ProductReviewDto>(Messages.ReviewUnauthorized);

        review.Rating = request.Rating;
        review.Comment = request.Comment.Trim();
        review.UpdatedAt = DateTime.UtcNow;

        _reviewDal.Update(review);
        await _unitOfWork.SaveChangesAsync();

        // Ortalama puanı ve yorum sayısını güncelle
        await UpdateProductRatingStats(review.ProductId);

        return new SuccessDataResult<ProductReviewDto>(MapToDto(review));
    }

    public async Task<IResult> DeleteAsync(int userId, int reviewId)
    {
        var review = await _reviewDal.GetAsync(r => r.Id == reviewId);
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
        var review = await _reviewDal.GetAsync(r => r.Id == reviewId);
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
        var reviews = await _reviewDal.GetByProductIdAsync(productId);
        return new SuccessDataResult<List<ProductReviewDto>>(
            reviews.Select(MapToDto).ToList());
    }

    public async Task<IDataResult<ProductReviewSummaryDto>> GetProductReviewSummaryAsync(int productId)
    {
        var reviews = await _reviewDal.GetByProductIdAsync(productId);
        var summary = new ProductReviewSummaryDto
        {
            AverageRating = reviews.Count > 0 ? reviews.Average(r => r.Rating) : 0,
            TotalReviews = reviews.Count,
            RatingDistribution = Enumerable.Range(1, 5)
                .ToDictionary(star => star, star => reviews.Count(r => r.Rating == star))
        };
        return new SuccessDataResult<ProductReviewSummaryDto>(summary);
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
        UserId = review.UserId,
        UserFullName = review.User != null
            ? $"{review.User.FirstName} {review.User.LastName}".Trim()
            : "Anonim",
        Rating = review.Rating,
        Comment = review.Comment,
        CreatedAt = review.CreatedAt,
        UpdatedAt = review.UpdatedAt,
    };
}
