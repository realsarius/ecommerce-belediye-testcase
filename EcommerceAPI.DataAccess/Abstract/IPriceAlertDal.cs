using EcommerceAPI.Core.DataAccess;
using EcommerceAPI.Entities.Concrete;

namespace EcommerceAPI.DataAccess.Abstract;

public interface IPriceAlertDal : IEntityRepository<PriceAlert>
{
    Task<PriceAlert?> GetByUserAndProductAsync(int userId, int productId);
    Task<IList<PriceAlert>> GetUserAlertsWithProductsAsync(int userId);
    Task<IList<PriceAlert>> GetActiveAlertsWithProductsAsync();
}
