using EcommerceAPI.Core.DataAccess.EntityFramework;
using EcommerceAPI.DataAccess.Abstract;
using EcommerceAPI.DataAccess.Concrete.EntityFramework.Contexts;
using EcommerceAPI.Entities.Concrete;
using Microsoft.EntityFrameworkCore;

namespace EcommerceAPI.DataAccess.Concrete.EntityFramework;

public class EfNotificationDal : EfEntityRepositoryBase<Notification, AppDbContext>, INotificationDal
{
    public EfNotificationDal(AppDbContext context) : base(context)
    {
    }

    public async Task<IList<Notification>> GetUserNotificationsAsync(int userId, int take = 50)
    {
        return await _context.Notifications
            .AsNoTracking()
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .Take(take)
            .ToListAsync();
    }

    public Task<int> CountUnreadAsync(int userId)
    {
        return _context.Notifications.CountAsync(n => n.UserId == userId && !n.IsRead);
    }

    public Task<Notification?> GetByIdForUserAsync(int id, int userId)
    {
        return _context.Notifications.FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId);
    }

    public Task<int> MarkAllAsReadAsync(int userId, DateTime readAtUtc)
    {
        return _context.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(n => n.IsRead, true)
                .SetProperty(n => n.ReadAt, readAtUtc)
                .SetProperty(n => n.UpdatedAt, readAtUtc));
    }
}
