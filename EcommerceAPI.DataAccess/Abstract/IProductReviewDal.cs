using EcommerceAPI.Core.DataAccess;
using EcommerceAPI.Entities.Concrete;

namespace EcommerceAPI.DataAccess.Abstract;

public interface IProductReviewDal : IEntityRepository<ProductReview>
{
    Task<ProductReview?> GetByUserAndProductAsync(int userId, int productId);
    Task<List<ProductReview>> GetByProductIdAsync(int productId);
    Task<(double average, int count)> GetProductRatingAsync(int productId);
}
