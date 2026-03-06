using EcommerceAPI.Core.Utilities.Results;
using EcommerceAPI.Entities.DTOs;

namespace EcommerceAPI.Business.Abstract;

public interface IAdminUserService
{
    Task<IDataResult<PaginatedResponse<AdminUserListItemDto>>> GetUsersAsync(AdminUsersQueryRequest request);
    Task<IDataResult<AdminUserDetailDto>> GetUserDetailAsync(int userId);
    Task<IDataResult<AdminUserDetailDto>> UpdateUserRoleAsync(int userId, UpdateUserRoleRequest request);
    Task<IDataResult<AdminUserDetailDto>> UpdateUserStatusAsync(int userId, UpdateUserStatusRequest request);
}
