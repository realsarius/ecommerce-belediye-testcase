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
        // AsNoTracking() metodu, verilerin sadece okunacağı durumlarda Change Tracker mekanizmasını devre dışı bırakarak performans artışı sağlar.
        return await _dbSet.AsNoTracking().FirstOrDefaultAsync(c => c.Name == name);
    }

    public async Task<IList<Category>> GetActiveCategoriesAsync()
    {
        return await _dbSet.Where(c => c.IsActive).AsNoTracking().ToListAsync();
    }
}
