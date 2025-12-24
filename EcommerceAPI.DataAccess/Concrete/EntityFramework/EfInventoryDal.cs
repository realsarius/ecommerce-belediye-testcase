using EcommerceAPI.Core.DataAccess.EntityFramework;
using EcommerceAPI.DataAccess.Abstract;
using EcommerceAPI.Entities.Concrete;
using Microsoft.EntityFrameworkCore;

using EcommerceAPI.DataAccess.Concrete.EntityFramework.Contexts;

namespace EcommerceAPI.DataAccess.Concrete.EntityFramework;

public class EfInventoryDal : EfEntityRepositoryBase<Inventory, AppDbContext>, IInventoryDal
{
    public EfInventoryDal(AppDbContext context) : base(context) { }

    public async Task<Inventory?> GetByProductIdAsync(int productId)
    {
        return await _dbSet.FirstOrDefaultAsync(i => i.ProductId == productId);
    }

    public async Task<IList<Inventory>> GetLowStockAsync(int threshold)
    {
        return await _dbSet
            .Include(i => i.Product)
            .Where(i => i.QuantityAvailable <= threshold)
            .ToListAsync();
    }

    public async Task AddMovementAsync(InventoryMovement movement)
    {
        await _context.Set<InventoryMovement>().AddAsync(movement);
    }
}
