using EcommerceAPI.Core.DataAccess.EntityFramework;
using EcommerceAPI.DataAccess.Abstract;
using EcommerceAPI.DataAccess.Concrete.EntityFramework.Contexts;
using EcommerceAPI.Entities.Concrete;
using Microsoft.EntityFrameworkCore;

namespace EcommerceAPI.DataAccess.Concrete.EntityFramework;

public class EfWishlistItemDal : EfEntityRepositoryBase<WishlistItem, AppDbContext>, IWishlistItemDal
{
    public EfWishlistItemDal(AppDbContext context) : base(context)
    {
    }

    public async Task<bool> AddIfNotExistsAsync(WishlistItem item)
    {
        var affectedRows = await _context.Database.ExecuteSqlInterpolatedAsync($@"
            INSERT INTO ""TBL_WishlistItems""
                (""WishlistId"", ""ProductId"", ""AddedAtPrice"", ""AddedAt"", ""CreatedAt"", ""UpdatedAt"")
            VALUES
                ({item.WishlistId}, {item.ProductId}, {item.AddedAtPrice}, {item.AddedAt}, {item.CreatedAt}, {item.UpdatedAt})
            ON CONFLICT (""WishlistId"", ""ProductId"") DO NOTHING");

        return affectedRows > 0;
    }

    public async Task<int> DeleteByWishlistAndProductAsync(int wishlistId, int productId)
    {
        return await _context.Database.ExecuteSqlInterpolatedAsync($@"
            DELETE FROM ""TBL_WishlistItems""
            WHERE ""WishlistId"" = {wishlistId}
              AND ""ProductId"" = {productId}");
    }

    public async Task<IList<WishlistItem>> GetPagedByWishlistIdAsync(
        int wishlistId,
        DateTime? cursorAddedAt,
        int? cursorItemId,
        int take)
    {
        var query = _context.WishlistItems
            .AsNoTracking()
            .Include(x => x.Product)
            .Where(x => x.WishlistId == wishlistId);

        if (cursorAddedAt.HasValue && cursorItemId.HasValue)
        {
            var cursorDate = cursorAddedAt.Value;
            var cursorId = cursorItemId.Value;

            query = query.Where(x =>
                x.AddedAt < cursorDate ||
                (x.AddedAt == cursorDate && x.Id < cursorId));
        }

        return await query
            .OrderByDescending(x => x.AddedAt)
            .ThenByDescending(x => x.Id)
            .Take(take)
            .ToListAsync();
    }
}
