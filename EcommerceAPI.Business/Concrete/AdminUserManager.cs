using EcommerceAPI.Application.Abstractions.ServiceContracts;
using EcommerceAPI.Business.Extensions;
using EcommerceAPI.Core.Utilities.Results;
using EcommerceAPI.Core.Interfaces;
using EcommerceAPI.Application.Abstractions.Persistence;
using EcommerceAPI.Entities.Concrete;
using EcommerceAPI.Entities.DTOs;
using EcommerceAPI.Entities.Enums;

namespace EcommerceAPI.Business.Concrete;

public class AdminUserManager : IAdminUserService
{
    private readonly IUserDal _userDal;
    private readonly IRoleDal _roleDal;
    private readonly IRefreshTokenDal _refreshTokenDal;
    private readonly IUnitOfWork _unitOfWork;

    public AdminUserManager(
        IUserDal userDal,
        IRoleDal roleDal,
        IRefreshTokenDal refreshTokenDal,
        IUnitOfWork unitOfWork)
    {
        _userDal = userDal;
        _roleDal = roleDal;
        _refreshTokenDal = refreshTokenDal;
        _unitOfWork = unitOfWork;
    }

    public async Task<IDataResult<PaginatedResponse<AdminUserListItemDto>>> GetUsersAsync(AdminUsersQueryRequest request)
    {
        var users = await _userDal.GetAdminUsersWithDetailsAsync();
        var query = users.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim();
            var lowerTerm = term.ToLowerInvariant();

            query = query.Where(user =>
                user.Email.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                user.FirstName.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                user.LastName.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                $"{user.FirstName} {user.LastName}".Contains(term, StringComparison.OrdinalIgnoreCase) ||
                user.Role.Name.Contains(term, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(request.Role))
        {
            query = query.Where(user => string.Equals(user.Role.Name, request.Role, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(request.Status) &&
            Enum.TryParse<UserAccountStatus>(request.Status, true, out var parsedStatus))
        {
            query = query.Where(user => user.AccountStatus == parsedStatus);
        }

        if (request.RegisteredFrom.HasValue)
        {
            query = query.Where(user => user.CreatedAt >= request.RegisteredFrom.Value);
        }

        if (request.RegisteredTo.HasValue)
        {
            var inclusiveTo = request.RegisteredTo.Value.Date.AddDays(1).AddTicks(-1);
            query = query.Where(user => user.CreatedAt <= inclusiveTo);
        }

        query = query.OrderByDescending(user => user.CreatedAt);

        var page = request.Page <= 0 ? 1 : request.Page;
        var pageSize = request.PageSize <= 0 ? 20 : Math.Min(request.PageSize, 100);
        var totalCount = query.Count();
        var items = query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(MapListItem)
            .ToList();

        return new SuccessDataResult<PaginatedResponse<AdminUserListItemDto>>(new PaginatedResponse<AdminUserListItemDto>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        });
    }

    public async Task<IDataResult<AdminUserDetailDto>> GetUserDetailAsync(int userId)
    {
        var user = await _userDal.GetAdminUserDetailAsync(userId);
        if (user == null)
            return new ErrorDataResult<AdminUserDetailDto>("Kullanıcı bulunamadı");

        return new SuccessDataResult<AdminUserDetailDto>(MapDetail(user));
    }

    public async Task<IDataResult<AdminUserDetailDto>> UpdateUserRoleAsync(int userId, UpdateUserRoleRequest request)
    {
        var user = await _userDal.GetByIdWithRoleAsync(userId);
        if (user == null)
            return new ErrorDataResult<AdminUserDetailDto>("Kullanıcı bulunamadı");

        if (string.IsNullOrWhiteSpace(request.Role))
            return new ErrorDataResult<AdminUserDetailDto>("Rol bilgisi zorunludur");

        var role = (await _roleDal.GetListAsync(r => r.Name == request.Role.Trim())).FirstOrDefault();
        if (role == null)
            return new ErrorDataResult<AdminUserDetailDto>("Rol bulunamadı");

        user.RoleId = role.Id;
        user.UpdatedAt = DateTime.UtcNow;

        _userDal.Update(user);
        await _unitOfWork.SaveChangesAsync();

        var refreshedUser = await _userDal.GetAdminUserDetailAsync(userId);
        return new SuccessDataResult<AdminUserDetailDto>(MapDetail(refreshedUser!), "Kullanıcı rolü güncellendi");
    }

    public async Task<IDataResult<AdminUserDetailDto>> UpdateUserStatusAsync(int userId, UpdateUserStatusRequest request)
    {
        var user = await _userDal.GetByIdWithRoleAsync(userId);
        if (user == null)
            return new ErrorDataResult<AdminUserDetailDto>("Kullanıcı bulunamadı");

        if (!Enum.TryParse<UserAccountStatus>(request.Status, true, out var status))
            return new ErrorDataResult<AdminUserDetailDto>("Geçersiz kullanıcı durumu");

        user.AccountStatus = status;
        user.UpdatedAt = DateTime.UtcNow;

        _userDal.Update(user);

        if (status != UserAccountStatus.Active)
        {
            var refreshTokens = await _refreshTokenDal.GetListAsync(token => token.UserId == userId && !token.IsRevoked);
            foreach (var token in refreshTokens)
            {
                token.IsRevoked = true;
                token.RevokedReason = $"Admin tarafından hesap durumu {status} yapıldı";
                _refreshTokenDal.Update(token);
            }
        }

        await _unitOfWork.SaveChangesAsync();

        var refreshedUser = await _userDal.GetAdminUserDetailAsync(userId);
        return new SuccessDataResult<AdminUserDetailDto>(MapDetail(refreshedUser!), "Kullanıcı durumu güncellendi");
    }

    private static AdminUserListItemDto MapListItem(User user)
    {
        var completedOrders = GetCompletedOrders(user);

        return new AdminUserListItemDto
        {
            Id = user.Id,
            FullName = $"{user.FirstName} {user.LastName}".Trim(),
            Email = user.Email,
            Role = user.Role?.Name ?? string.Empty,
            Status = user.AccountStatus.ToString(),
            CreatedAt = user.CreatedAt,
            LastLoginAt = user.LastLoginAt,
            TotalSpent = completedOrders.Sum(order => order.TotalAmount),
            OrderCount = completedOrders.Count,
            IsEmailVerified = user.IsEmailVerified
        };
    }

    private static AdminUserDetailDto MapDetail(User user)
    {
        var completedOrders = GetCompletedOrders(user);
        var totalSpent = completedOrders.Sum(order => order.TotalAmount);
        var orderCount = completedOrders.Count;

        return new AdminUserDetailDto
        {
            Id = user.Id,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            FullName = $"{user.FirstName} {user.LastName}".Trim(),
            Role = user.Role?.Name ?? string.Empty,
            Status = user.AccountStatus.ToString(),
            IsEmailVerified = user.IsEmailVerified,
            CreatedAt = user.CreatedAt,
            LastLoginAt = user.LastLoginAt,
            TotalSpent = totalSpent,
            AverageOrderValue = orderCount == 0 ? 0 : totalSpent / orderCount,
            OrderCount = orderCount,
            Orders = user.Orders
                .OrderByDescending(order => order.CreatedAt)
                .Select(order => order.ToDto())
                .ToList(),
            Addresses = user.ShippingAddresses
                .OrderByDescending(address => address.IsDefault)
                .ThenBy(address => address.Title)
                .Select(address => new ShippingAddressDto
                {
                    Id = address.Id,
                    Title = address.Title,
                    FullName = address.FullName,
                    Phone = address.Phone,
                    City = address.City,
                    District = address.District,
                    AddressLine = address.AddressLine,
                    PostalCode = address.PostalCode,
                    IsDefault = address.IsDefault
                })
                .ToList()
        };
    }

    private static List<Order> GetCompletedOrders(User user)
    {
        return user.Orders
            .Where(order => order.Status != OrderStatus.PendingPayment && order.Status != OrderStatus.Cancelled)
            .ToList();
    }
}
