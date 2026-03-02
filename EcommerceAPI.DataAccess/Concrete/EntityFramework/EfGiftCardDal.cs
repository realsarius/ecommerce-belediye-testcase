using EcommerceAPI.Core.DataAccess.EntityFramework;
using EcommerceAPI.DataAccess.Abstract;
using EcommerceAPI.DataAccess.Concrete.EntityFramework.Contexts;
using EcommerceAPI.Entities.Concrete;
using Microsoft.EntityFrameworkCore;

namespace EcommerceAPI.DataAccess.Concrete.EntityFramework;

public class EfGiftCardDal : EfEntityRepositoryBase<GiftCard, AppDbContext>, IGiftCardDal
{
    public EfGiftCardDal(AppDbContext context) : base(context)
    {
    }

    public async Task<GiftCard?> GetByCodeAsync(string code)
    {
        return await _dbSet
            .Include(x => x.AssignedUser)
            .FirstOrDefaultAsync(x => x.Code == code);
    }

    public async Task<GiftCard?> GetByIdWithAssignedUserAsync(int id)
    {
        return await _dbSet
            .Include(x => x.AssignedUser)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id);
    }

    public async Task<IList<GiftCard>> GetAllWithAssignedUserAsync()
    {
        return await _dbSet
            .Include(x => x.AssignedUser)
            .OrderByDescending(x => x.CreatedAt)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<IList<GiftCard>> GetUserGiftCardsAsync(int userId)
    {
        return await _dbSet
            .Include(x => x.AssignedUser)
            .Where(x => x.AssignedUserId == userId)
            .OrderByDescending(x => x.CreatedAt)
            .AsNoTracking()
            .ToListAsync();
    }
}
