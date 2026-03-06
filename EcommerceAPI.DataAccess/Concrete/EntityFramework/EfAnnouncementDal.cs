using EcommerceAPI.Core.DataAccess.EntityFramework;
using EcommerceAPI.Application.Abstractions.Persistence;
using EcommerceAPI.DataAccess.Concrete.EntityFramework.Contexts;
using EcommerceAPI.Entities.Concrete;
using Microsoft.EntityFrameworkCore;

namespace EcommerceAPI.DataAccess.Concrete.EntityFramework;

public class EfAnnouncementDal : EfEntityRepositoryBase<Announcement, AppDbContext>, IAnnouncementDal
{
    public EfAnnouncementDal(AppDbContext context) : base(context)
    {
    }

    public async Task<Announcement?> GetByIdWithCreatorAsync(int id)
    {
        return await _dbSet
            .Include(announcement => announcement.CreatedByUser)
            .FirstOrDefaultAsync(announcement => announcement.Id == id);
    }

    public async Task<List<Announcement>> GetRecentWithCreatorAsync(int take = 20)
    {
        return await _dbSet
            .Include(announcement => announcement.CreatedByUser)
            .OrderByDescending(announcement => announcement.CreatedAt)
            .Take(Math.Clamp(take, 1, 100))
            .ToListAsync();
    }
}
