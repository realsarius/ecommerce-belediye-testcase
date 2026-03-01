using EcommerceAPI.Core.DataAccess;
using EcommerceAPI.Entities.Concrete;

namespace EcommerceAPI.DataAccess.Abstract;

public interface ICampaignDal : IEntityRepository<Campaign>
{
    Task<Campaign?> GetByIdWithProductsAsync(int id);
    Task<List<Campaign>> GetActiveCampaignsAsync(DateTime utcNow);
    Task<List<Campaign>> GetAllWithProductsAsync();
    Task<bool> HasOverlappingProductCampaignsAsync(IEnumerable<int> productIds, DateTime startsAt, DateTime endsAt, int? excludeCampaignId = null);
}
