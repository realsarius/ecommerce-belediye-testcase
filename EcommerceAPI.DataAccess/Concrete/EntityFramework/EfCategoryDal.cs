using EcommerceAPI.Core.DataAccess.EntityFramework;
using EcommerceAPI.DataAccess.Abstract;
using EcommerceAPI.Entities.Concrete;
using Microsoft.EntityFrameworkCore;

using EcommerceAPI.DataAccess.Concrete.EntityFramework.Contexts;

namespace EcommerceAPI.DataAccess.Concrete.EntityFramework;

public class EfCategoryDal : EfEntityRepositoryBase<Category, AppDbContext>, ICategoryDal
{
    public EfCategoryDal(AppDbContext context) : base(context) { }

    public async Task<Category?> GetByNameAsync(string name)
    {

        return await _dbSet.AsNoTracking().FirstOrDefaultAsync(c => c.Name == name);
    }

    public async Task<IList<Category>> GetActiveCategoriesAsync()
    {
        return await _dbSet
            .Include(c => c.Products)
            .Include(c => c.Children)
            .Where(c => c.IsActive)
            .OrderBy(c => c.ParentCategoryId ?? 0)
            .ThenBy(c => c.SortOrder)
            .ThenBy(c => c.Name)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<IList<Category>> GetAllWithProductsAsync()
    {
        return await _dbSet
            .Include(c => c.Products)
            .Include(c => c.Children)
            .OrderBy(c => c.ParentCategoryId ?? 0)
            .ThenBy(c => c.SortOrder)
            .ThenBy(c => c.Name)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<IList<Category>> GetAllWithHierarchyAsync(bool includeInactive = false)
    {
        var query = _dbSet
            .Include(c => c.Products)
            .Include(c => c.Children)
            .AsQueryable();

        if (!includeInactive)
        {
            query = query.Where(c => c.IsActive);
        }

        return await query
            .OrderBy(c => c.ParentCategoryId ?? 0)
            .ThenBy(c => c.SortOrder)
            .ThenBy(c => c.Name)
            .AsNoTracking()
            .ToListAsync();
    }

    public Task<int> GetDashboardCategoryCountAsync()
    {
        return _dbSet.AsNoTracking().CountAsync();
    }
}
