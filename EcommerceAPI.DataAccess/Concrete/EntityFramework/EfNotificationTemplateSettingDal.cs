using EcommerceAPI.Core.DataAccess.EntityFramework;
using EcommerceAPI.Application.Abstractions.Persistence;
using EcommerceAPI.DataAccess.Concrete.EntityFramework.Contexts;
using EcommerceAPI.Entities.Concrete;
using EcommerceAPI.Entities.Enums;
using Microsoft.EntityFrameworkCore;

namespace EcommerceAPI.DataAccess.Concrete.EntityFramework;

public class EfNotificationTemplateSettingDal : EfEntityRepositoryBase<NotificationTemplateSetting, AppDbContext>, INotificationTemplateSettingDal
{
    public EfNotificationTemplateSettingDal(AppDbContext context) : base(context)
    {
    }

    public async Task<IList<NotificationTemplateSetting>> GetAllAsync()
    {
        return await _context.NotificationTemplateSettings
            .AsNoTracking()
            .OrderBy(x => x.Type)
            .ToListAsync();
    }

    public async Task<NotificationTemplateSetting?> GetByTypeAsync(NotificationType type)
    {
        return await _context.NotificationTemplateSettings
            .FirstOrDefaultAsync(x => x.Type == type);
    }
}
