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

    public async Task<User?> GetAdminUserDetailAsync(int id)
    {
        return await _dbSet
            .Include(u => u.Role)
            .Include(u => u.Orders)
                .ThenInclude(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
            .Include(u => u.Orders)
                .ThenInclude(o => o.Payment)
            .Include(u => u.ShippingAddresses)
            .FirstOrDefaultAsync(u => u.Id == id);
    }

    public async Task<List<User>> GetAdminUsersWithDetailsAsync()
    {
        return await _dbSet
            .Include(u => u.Role)
            .Include(u => u.Orders)
            .ToListAsync();
    }

    public async Task<List<User>> GetUsersWithRolesAsync()
    {
        return await _dbSet
            .Include(u => u.Role)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<DateTime>> GetAdminDashboardUserCreatedDatesAsync()
    {
        return await _dbSet
            .AsNoTracking()
            .Select(user => user.CreatedAt)
            .ToListAsync();
    }
}
