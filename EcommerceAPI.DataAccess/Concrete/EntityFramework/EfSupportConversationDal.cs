using EcommerceAPI.Core.DataAccess.EntityFramework;
using EcommerceAPI.DataAccess.Abstract;
using EcommerceAPI.DataAccess.Concrete.EntityFramework.Contexts;
using EcommerceAPI.Entities.Concrete;
using EcommerceAPI.Entities.Enums;
using Microsoft.EntityFrameworkCore;

namespace EcommerceAPI.DataAccess.Concrete.EntityFramework;

public class EfSupportConversationDal
    : EfEntityRepositoryBase<SupportConversation, AppDbContext>, ISupportConversationDal
{
    public EfSupportConversationDal(AppDbContext context) : base(context) { }

    public async Task<SupportConversation?> GetByIdWithDetailsAsync(int conversationId)
    {
        return await _dbSet
            .Include(x => x.CustomerUser)
            .Include(x => x.SupportUser)
            .Include(x => x.Messages.OrderBy(m => m.CreatedAt))
                .ThenInclude(m => m.SenderUser)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == conversationId);
    }

    public async Task<List<SupportConversation>> GetByCustomerUserIdAsync(int customerUserId, bool onlyOpen = false)
    {
        var query = _dbSet
            .Include(x => x.CustomerUser)
            .Include(x => x.SupportUser)
            .Where(x => x.CustomerUserId == customerUserId);

        if (onlyOpen)
        {
            query = query.Where(x => x.Status != SupportConversationStatus.Closed);
        }

        return await query
            .OrderByDescending(x => x.LastMessageAt ?? x.CreatedAt)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<List<SupportConversation>> GetQueueAsync(int page, int pageSize)
    {
        var skip = (page - 1) * pageSize;

        return await _dbSet
            .Include(x => x.CustomerUser)
            .Include(x => x.SupportUser)
            .Where(x => x.Status == SupportConversationStatus.Open)
            .OrderBy(x => x.CreatedAt)
            .Skip(skip)
            .Take(pageSize)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<List<SupportConversation>> GetQueueForSupportAsync(int supportUserId, int page, int pageSize)
    {
        var skip = (page - 1) * pageSize;

        return await _dbSet
            .Include(x => x.CustomerUser)
            .Include(x => x.SupportUser)
            .Where(x =>
                x.Status == SupportConversationStatus.Open ||
                (x.SupportUserId == supportUserId && x.Status != SupportConversationStatus.Closed))
            .OrderByDescending(x => x.SupportUserId == supportUserId)
            .ThenByDescending(x => x.LastMessageAt ?? x.CreatedAt)
            .Skip(skip)
            .Take(pageSize)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<List<SupportConversation>> GetAssignedToSupportAsync(int supportUserId, int page, int pageSize)
    {
        var skip = (page - 1) * pageSize;

        return await _dbSet
            .Include(x => x.CustomerUser)
            .Include(x => x.SupportUser)
            .Where(x => x.SupportUserId == supportUserId && x.Status != SupportConversationStatus.Closed)
            .OrderByDescending(x => x.LastMessageAt ?? x.CreatedAt)
            .Skip(skip)
            .Take(pageSize)
            .AsNoTracking()
            .ToListAsync();
    }
}
