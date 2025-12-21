using EcommerceAPI.Core.Entities;
using EcommerceAPI.Core.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EcommerceAPI.Data.Repositories;

public class CategoryRepository : GenericRepository<Category>, ICategoryRepository
{
    public CategoryRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<Category?> GetByNameAsync(string name)
    {
        return await _dbSet
            .FirstOrDefaultAsync(c => EF.Functions.ILike(c.Name, name));
    }

    public async Task<Category?> GetByIdWithProductsAsync(int id)
    {
        return await _dbSet
            .Include(c => c.Products)
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<IEnumerable<Category>> GetAllWithProductCountAsync(bool includeInactive = false)
    {
        var query = _dbSet.AsQueryable();
        
        if (!includeInactive)
        {
            query = query.Where(c => c.IsActive);
        }
        
        return await query
            .Include(c => c.Products)
            .OrderBy(c => c.Name)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<bool> HasProductsAsync(int categoryId)
    {
        return await _context.Set<Product>()
            .AnyAsync(p => p.CategoryId == categoryId);
    }
}
