using EcommerceAPI.Core.DataAccess;
using EcommerceAPI.Entities.Concrete;

namespace EcommerceAPI.DataAccess.Abstract;

public interface IUserDal : IEntityRepository<User>
{
    Task<User?> GetByIdWithRoleAsync(int id);
}
