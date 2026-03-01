using EcommerceAPI.Core.DataAccess.EntityFramework;
using EcommerceAPI.DataAccess.Abstract;
using EcommerceAPI.DataAccess.Concrete.EntityFramework.Contexts;
using EcommerceAPI.Entities.Concrete;
using EcommerceAPI.Entities.Enums;
using Microsoft.EntityFrameworkCore;

namespace EcommerceAPI.DataAccess.Concrete.EntityFramework;

public class EfCampaignDal : EfEntityRepositoryBase<Campaign, AppDbContext>, ICampaignDal
{
    public EfCampaignDal(AppDbContext context) : base(context)
    {
    }

    public async Task<Campaign?> GetByIdWithProductsAsync(int id)
    {
        return await _dbSet
            .Include(x => x.CampaignProducts)
            .ThenInclude(x => x.Product)
            .FirstOrDefaultAsync(x => x.Id == id);
    }

    public async Task<List<Campaign>> GetActiveCampaignsAsync(DateTime utcNow)
    {
        return await _dbSet
            .Include(x => x.CampaignProducts)
            .ThenInclude(x => x.Product)
            .Where(x =>
                x.IsEnabled &&
                x.Status == CampaignStatus.Active &&
                x.StartsAt <= utcNow &&
                x.EndsAt > utcNow)
            .AsNoTracking()
            .OrderBy(x => x.StartsAt)
            .ToListAsync();
    }

    public async Task<List<Campaign>> GetAllWithProductsAsync()
    {
        return await _dbSet
            .Include(x => x.CampaignProducts)
            .ThenInclude(x => x.Product)
            .AsNoTracking()
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();
    }

    public async Task<bool> HasOverlappingProductCampaignsAsync(IEnumerable<int> productIds, DateTime startsAt, DateTime endsAt, int? excludeCampaignId = null)
    {
        var normalizedProductIds = productIds
            .Where(x => x > 0)
            .Distinct()
            .ToList();

        if (normalizedProductIds.Count == 0)
        {
            return false;
        }

        var query = _dbSet
            .Where(x => x.IsEnabled && x.StartsAt < endsAt && x.EndsAt > startsAt)
            .Where(x => x.CampaignProducts.Any(cp => normalizedProductIds.Contains(cp.ProductId)));

        if (excludeCampaignId.HasValue)
        {
            query = query.Where(x => x.Id != excludeCampaignId.Value);
        }

        return await query.AnyAsync();
    }
}
