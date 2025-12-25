using EcommerceAPI.Core.DataAccess.EntityFramework;
using EcommerceAPI.DataAccess.Abstract;
using EcommerceAPI.DataAccess.Concrete.EntityFramework.Contexts;
using EcommerceAPI.Entities.Concrete;
using Microsoft.EntityFrameworkCore;

namespace EcommerceAPI.DataAccess.Concrete.EntityFramework;

public class EfUserDal : EfEntityRepositoryBase<User, AppDbContext>, IUserDal
{
    public EfUserDal(AppDbContext context) : base(context) { }

    public async Task<User?> GetByIdWithRoleAsync(int id)
    {
        return await _dbSet.Include(u => u.Role).FirstOrDefaultAsync(u => u.Id == id);
    }
}
