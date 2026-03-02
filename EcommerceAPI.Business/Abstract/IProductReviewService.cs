using EcommerceAPI.Core.Utilities.Results;
using EcommerceAPI.Entities.DTOs;
using EcommerceAPI.Entities.Enums;

namespace EcommerceAPI.Business.Abstract;

public interface IProductReviewService
{
    Task<IDataResult<List<ProductReviewDto>>> GetByProductIdAsync(int productId);
    Task<IDataResult<ProductReviewSummaryDto>> GetProductReviewSummaryAsync(int productId);
    Task<IDataResult<List<ProductReviewDto>>> GetAdminReviewsAsync(ProductReviewModerationStatus? moderationStatus = null);
    Task<IDataResult<ProductReviewDto>> CreateAsync(int userId, int productId, CreateReviewRequest request);
    Task<IDataResult<ProductReviewDto>> UpdateAsync(int userId, int reviewId, UpdateReviewRequest request);
    Task<IDataResult<ProductReviewDto>> SellerReplyAsync(int sellerUserId, int reviewId, SellerReviewReplyRequest request);
    Task<IDataResult<ProductReviewDto>> AdminApproveAsync(int reviewId, int adminUserId);
    Task<IDataResult<ProductReviewDto>> AdminRejectAsync(int reviewId, int adminUserId, ReviewModerationRequest request);
    Task<IResult> AdminBulkApproveAsync(IEnumerable<int> reviewIds, int adminUserId);
    Task<IResult> DeleteAsync(int userId, int reviewId);
    Task<IResult> AdminDeleteAsync(int reviewId);
    Task<IDataResult<bool>> CanUserReviewAsync(int userId, int productId);
}
