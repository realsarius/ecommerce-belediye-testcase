using EcommerceAPI.Core.DataAccess;
using EcommerceAPI.Entities.Concrete;

namespace EcommerceAPI.Application.Abstractions.Persistence;

public interface ISellerProfileDal : IEntityRepository<SellerProfile>
{
    Task<SellerProfile?> GetByUserIdWithDetailsAsync(int userId);
    Task<SellerProfile?> GetByIdWithDetailsAsync(int id);
    Task<List<SellerProfile>> GetAdminListWithDetailsAsync();
    Task<SellerProfile?> GetAdminDetailWithDetailsAsync(int id);
    Task<int> GetPendingApplicationCountAsync();
}
