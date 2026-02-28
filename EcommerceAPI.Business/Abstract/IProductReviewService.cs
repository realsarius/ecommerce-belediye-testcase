using EcommerceAPI.Core.Utilities.Results;
using EcommerceAPI.Entities.DTOs;

namespace EcommerceAPI.Business.Abstract;

public interface IProductReviewService
{
    Task<IDataResult<List<ProductReviewDto>>> GetByProductIdAsync(int productId);
    Task<IDataResult<ProductReviewSummaryDto>> GetProductReviewSummaryAsync(int productId);
    Task<IDataResult<ProductReviewDto>> CreateAsync(int userId, int productId, CreateReviewRequest request);
    Task<IDataResult<ProductReviewDto>> UpdateAsync(int userId, int reviewId, UpdateReviewRequest request);
    Task<IResult> DeleteAsync(int userId, int reviewId);
    Task<IResult> AdminDeleteAsync(int reviewId);
    Task<IDataResult<bool>> CanUserReviewAsync(int userId, int productId);
}
