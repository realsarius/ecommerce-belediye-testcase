using EcommerceAPI.Core.DataAccess.EntityFramework;
using EcommerceAPI.DataAccess.Abstract;
using EcommerceAPI.DataAccess.Concrete.EntityFramework.Contexts;
using EcommerceAPI.Entities.Concrete;
using Microsoft.EntityFrameworkCore;

namespace EcommerceAPI.DataAccess.Concrete.EntityFramework;

public class EfProductReviewDal : EfEntityRepositoryBase<ProductReview, AppDbContext>, IProductReviewDal
{
    public EfProductReviewDal(AppDbContext context) : base(context)
    {
    }

    public async Task<ProductReview?> GetByUserAndProductAsync(int userId, int productId)
    {
        return await _context.ProductReviews
            .Include(r => r.User)
            .FirstOrDefaultAsync(r => r.UserId == userId && r.ProductId == productId);
    }

    public async Task<List<ProductReview>> GetByProductIdAsync(int productId)
    {
        return await _context.ProductReviews
            .Include(r => r.User)
            .Where(r => r.ProductId == productId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
    }

    public async Task<(double average, int count)> GetProductRatingAsync(int productId)
    {
        var reviews = await _context.ProductReviews
            .Where(r => r.ProductId == productId)
            .ToListAsync();

        if (reviews.Count == 0)
            return (0, 0);

        return (reviews.Average(r => r.Rating), reviews.Count);
    }
}
