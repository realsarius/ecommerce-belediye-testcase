using EcommerceAPI.Core.DataAccess.EntityFramework;
using EcommerceAPI.Application.Abstractions.Persistence;
using EcommerceAPI.DataAccess.Concrete.EntityFramework.Contexts;
using EcommerceAPI.Entities.Concrete;
using EcommerceAPI.Entities.Enums;
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

    public async Task<ProductReview?> GetByIdWithDetailsAsync(int reviewId)
    {
        return await _context.ProductReviews
            .Include(r => r.User)
            .Include(r => r.Product)
            .FirstOrDefaultAsync(r => r.Id == reviewId);
    }

    public async Task<List<ProductReview>> GetByProductIdAsync(int productId, ProductReviewModerationStatus? moderationStatus = null)
    {
        var query = _context.ProductReviews
            .Include(r => r.User)
            .Include(r => r.Product)
            .Where(r => r.ProductId == productId);

        if (moderationStatus.HasValue)
        {
            query = query.Where(r => r.ModerationStatus == moderationStatus.Value);
        }

        return await query
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<ProductReview>> GetAdminListAsync(ProductReviewModerationStatus? moderationStatus = null)
    {
        var query = _context.ProductReviews
            .Include(r => r.User)
            .Include(r => r.Product)
            .AsQueryable();

        if (moderationStatus.HasValue)
        {
            query = query.Where(r => r.ModerationStatus == moderationStatus.Value);
        }

        return await query
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<ProductReview>> GetSellerListAsync(int sellerProfileId, int? productId = null, int? rating = null, bool? replied = null)
    {
        var query = _context.ProductReviews
            .Include(r => r.User)
            .Include(r => r.Product)
            .Where(r => r.Product != null
                        && r.Product.SellerId == sellerProfileId
                        && r.ModerationStatus == ProductReviewModerationStatus.Approved)
            .AsQueryable();

        if (productId.HasValue)
        {
            query = query.Where(r => r.ProductId == productId.Value);
        }

        if (rating.HasValue)
        {
            query = query.Where(r => r.Rating == rating.Value);
        }

        if (replied.HasValue)
        {
            query = replied.Value
                ? query.Where(r => !string.IsNullOrWhiteSpace(r.SellerReply))
                : query.Where(r => string.IsNullOrWhiteSpace(r.SellerReply));
        }

        return await query
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
