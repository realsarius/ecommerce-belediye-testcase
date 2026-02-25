using EcommerceAPI.Core.DataAccess.EntityFramework;
using EcommerceAPI.DataAccess.Abstract;
using EcommerceAPI.DataAccess.Concrete.EntityFramework.Contexts;
using EcommerceAPI.Entities.Concrete;
using Microsoft.EntityFrameworkCore;

namespace EcommerceAPI.DataAccess.Concrete.EntityFramework;

public class EfSupportMessageDal
    : EfEntityRepositoryBase<SupportMessage, AppDbContext>, ISupportMessageDal
{
    public EfSupportMessageDal(AppDbContext context) : base(context) { }

    public async Task<List<SupportMessage>> GetConversationMessagesAsync(int conversationId, int page, int pageSize)
    {
        var skip = (page - 1) * pageSize;

        return await _dbSet
            .Include(x => x.SenderUser)
            .Where(x => x.ConversationId == conversationId)
            .OrderBy(x => x.CreatedAt)
            .Skip(skip)
            .Take(pageSize)
            .AsNoTracking()
            .ToListAsync();
    }
}
