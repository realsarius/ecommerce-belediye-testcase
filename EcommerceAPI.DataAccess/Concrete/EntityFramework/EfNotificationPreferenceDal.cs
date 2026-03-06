using EcommerceAPI.Core.DataAccess.EntityFramework;
using EcommerceAPI.Application.Abstractions.Persistence;
using EcommerceAPI.DataAccess.Concrete.EntityFramework.Contexts;
using EcommerceAPI.Entities.Concrete;
using EcommerceAPI.Entities.Enums;
using Microsoft.EntityFrameworkCore;

namespace EcommerceAPI.DataAccess.Concrete.EntityFramework;

public class EfNotificationPreferenceDal : EfEntityRepositoryBase<NotificationPreference, AppDbContext>, INotificationPreferenceDal
{
    public EfNotificationPreferenceDal(AppDbContext context) : base(context)
    {
    }

    public async Task<IList<NotificationPreference>> GetByUserIdAsync(int userId)
    {
        return await _context.NotificationPreferences
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .OrderBy(x => x.Type)
            .ToListAsync();
    }

    public async Task<IList<NotificationPreference>> GetByUserIdsAndTypeAsync(IEnumerable<int> userIds, NotificationType type)
    {
        var ids = userIds.Distinct().ToList();
        if (ids.Count == 0)
        {
            return [];
        }

        return await _context.NotificationPreferences
            .AsNoTracking()
            .Where(x => ids.Contains(x.UserId) && x.Type == type)
            .ToListAsync();
    }
}
