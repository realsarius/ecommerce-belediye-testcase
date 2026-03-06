using EcommerceAPI.Core.DataAccess;
using EcommerceAPI.Entities.Concrete;

namespace EcommerceAPI.DataAccess.Abstract;

public interface IUserDal : IEntityRepository<User>
{
    Task<User?> GetByIdWithRoleAsync(int id);
    Task<User?> GetAdminUserDetailAsync(int id);
    Task<List<User>> GetAdminUsersWithDetailsAsync();
    Task<List<User>> GetUsersWithRolesAsync();
    Task<IReadOnlyList<DateTime>> GetAdminDashboardUserCreatedDatesAsync();
}
