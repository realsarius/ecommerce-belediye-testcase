using EcommerceAPI.Core.DataAccess;
using EcommerceAPI.Entities.Concrete;

namespace EcommerceAPI.DataAccess.Abstract;

public interface ISellerProfileDal : IEntityRepository<SellerProfile>
{
    Task<SellerProfile?> GetByUserIdWithDetailsAsync(int userId);
    Task<SellerProfile?> GetByIdWithDetailsAsync(int id);
}
