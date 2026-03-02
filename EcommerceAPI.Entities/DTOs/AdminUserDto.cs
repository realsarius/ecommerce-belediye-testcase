using EcommerceAPI.Core.Entities;

namespace EcommerceAPI.Entities.DTOs;

public class AdminUserListItemDto : IDto
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public decimal TotalSpent { get; set; }
    public int OrderCount { get; set; }
    public bool IsEmailVerified { get; set; }
}

public class AdminUserDetailDto : IDto
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public bool IsEmailVerified { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public decimal TotalSpent { get; set; }
    public decimal AverageOrderValue { get; set; }
    public int OrderCount { get; set; }
    public List<OrderDto> Orders { get; set; } = new();
    public List<ShippingAddressDto> Addresses { get; set; } = new();
}

public class AdminUsersQueryRequest : IDto
{
    public string? Search { get; set; }
    public string? Role { get; set; }
    public string? Status { get; set; }
    public DateTime? RegisteredFrom { get; set; }
    public DateTime? RegisteredTo { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class UpdateUserRoleRequest : IDto
{
    public string Role { get; set; } = string.Empty;
}

public class UpdateUserStatusRequest : IDto
{
    public string Status { get; set; } = string.Empty;
}
