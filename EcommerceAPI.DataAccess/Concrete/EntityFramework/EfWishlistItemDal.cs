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
}
