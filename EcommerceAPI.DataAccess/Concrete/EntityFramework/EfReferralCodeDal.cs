using EcommerceAPI.Core.DataAccess.EntityFramework;
using EcommerceAPI.Application.Abstractions.Persistence;
using EcommerceAPI.DataAccess.Concrete.EntityFramework.Contexts;
using EcommerceAPI.Entities.Concrete;
using Microsoft.EntityFrameworkCore;

namespace EcommerceAPI.DataAccess.Concrete.EntityFramework;

public class EfReferralCodeDal : EfEntityRepositoryBase<ReferralCode, AppDbContext>, IReferralCodeDal
{
    public EfReferralCodeDal(AppDbContext context) : base(context)
    {
    }

    public async Task<ReferralCode?> GetByCodeAsync(string code)
    {
        return await _dbSet
            .Include(x => x.User)
            .FirstOrDefaultAsync(x => x.Code == code);
    }

    public async Task<ReferralCode?> GetByUserIdAsync(int userId)
    {
        return await _dbSet
            .Include(x => x.User)
            .FirstOrDefaultAsync(x => x.UserId == userId);
    }
}
