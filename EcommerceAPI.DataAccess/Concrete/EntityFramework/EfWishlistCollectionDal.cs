using EcommerceAPI.Core.DataAccess.EntityFramework;
using EcommerceAPI.DataAccess.Abstract;
using EcommerceAPI.DataAccess.Concrete.EntityFramework.Contexts;
using EcommerceAPI.Entities.Concrete;
using Microsoft.EntityFrameworkCore;

namespace EcommerceAPI.DataAccess.Concrete.EntityFramework;

public class EfWishlistCollectionDal : EfEntityRepositoryBase<WishlistCollection, AppDbContext>, IWishlistCollectionDal
{
    public EfWishlistCollectionDal(AppDbContext context) : base(context)
    {
    }

    public async Task<WishlistCollection> GetOrCreateDefaultCollectionAsync(int wishlistId)
    {
        var existingCollection = await _context.Set<WishlistCollection>()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.WishlistId == wishlistId && x.IsDefault);

        if (existingCollection != null)
        {
            return existingCollection;
        }

        var now = DateTime.UtcNow;
        await _context.Database.ExecuteSqlInterpolatedAsync($@"
            INSERT INTO ""TBL_WishlistCollections"" (""WishlistId"", ""Name"", ""IsDefault"", ""CreatedAt"", ""UpdatedAt"")
            VALUES ({wishlistId}, {"Favorilerim"}, {true}, {now}, {now})
            ON CONFLICT (""WishlistId"") WHERE ""IsDefault"" = true DO NOTHING");

        return await _context.Set<WishlistCollection>()
            .AsNoTracking()
            .FirstAsync(x => x.WishlistId == wishlistId && x.IsDefault);
    }

    public async Task<IList<WishlistCollection>> GetByWishlistIdAsync(int wishlistId)
    {
        return await _context.Set<WishlistCollection>()
            .AsNoTracking()
            .Where(x => x.WishlistId == wishlistId)
            .OrderByDescending(x => x.IsDefault)
            .ThenBy(x => x.Name)
            .ToListAsync();
    }

    public async Task<bool> ExistsByNameAsync(int wishlistId, string name)
    {
        var normalizedName = name.Trim().ToLower();
        return await _context.Set<WishlistCollection>()
            .AsNoTracking()
            .AnyAsync(x => x.WishlistId == wishlistId && x.Name.ToLower() == normalizedName);
    }
}
