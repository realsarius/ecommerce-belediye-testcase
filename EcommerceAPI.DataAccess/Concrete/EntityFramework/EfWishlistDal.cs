using EcommerceAPI.Core.DataAccess.EntityFramework;
using EcommerceAPI.DataAccess.Abstract;
using EcommerceAPI.DataAccess.Concrete.EntityFramework.Contexts;
using EcommerceAPI.Entities.Concrete;
using Microsoft.EntityFrameworkCore;

namespace EcommerceAPI.DataAccess.Concrete.EntityFramework;

public class EfWishlistDal : EfEntityRepositoryBase<Wishlist, AppDbContext>, IWishlistDal
{
    public EfWishlistDal(AppDbContext context) : base(context)
    {
    }

    public async Task<Wishlist> GetOrCreateByUserIdAsync(int userId)
    {
        var existingWishlist = await _context.Wishlists
            .AsNoTracking()
            .FirstOrDefaultAsync(w => w.UserId == userId);

        if (existingWishlist != null)
        {
            return existingWishlist;
        }

        var now = DateTime.UtcNow;

        await _context.Database.ExecuteSqlInterpolatedAsync($@"
            INSERT INTO ""TBL_Wishlists"" (""UserId"", ""CreatedAt"", ""UpdatedAt"")
            VALUES ({userId}, {now}, {now})
            ON CONFLICT (""UserId"") DO NOTHING");

        return await _context.Wishlists
            .AsNoTracking()
            .FirstAsync(w => w.UserId == userId);
    }

    public async Task<Wishlist?> GetByShareTokenAsync(Guid shareToken)
    {
        return await _context.Wishlists
            .AsNoTracking()
            .Include(w => w.User)
            .FirstOrDefaultAsync(w => w.ShareToken == shareToken);
    }
}
