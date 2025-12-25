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
            .Where(c => c.IsActive)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<IList<Category>> GetAllWithProductsAsync()
    {
        return await _dbSet
            .Include(c => c.Products)
            .AsNoTracking()
            .ToListAsync();
    }
}
