using EcommerceAPI.Core.DataAccess;
using EcommerceAPI.Entities.Concrete;
using EcommerceAPI.Entities.Enums;

namespace EcommerceAPI.Application.Abstractions.Persistence;

public interface IProductReviewDal : IEntityRepository<ProductReview>
{
    Task<ProductReview?> GetByUserAndProductAsync(int userId, int productId);
    Task<ProductReview?> GetByIdWithDetailsAsync(int reviewId);
    Task<List<ProductReview>> GetByProductIdAsync(int productId, ProductReviewModerationStatus? moderationStatus = null);
    Task<List<ProductReview>> GetAdminListAsync(ProductReviewModerationStatus? moderationStatus = null);
    Task<List<ProductReview>> GetSellerListAsync(int sellerProfileId, int? productId = null, int? rating = null, bool? replied = null);
    Task<(double average, int count)> GetProductRatingAsync(int productId);
}
