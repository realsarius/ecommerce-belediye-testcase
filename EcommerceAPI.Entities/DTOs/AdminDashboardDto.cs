using EcommerceAPI.Core.Entities;
using EcommerceAPI.Entities.Enums;

namespace EcommerceAPI.Entities.DTOs;

public class AdminDashboardKpiDto : IDto
{
    public decimal TodayRevenue { get; set; }
    public decimal YesterdayRevenue { get; set; }
    public int TodayOrders { get; set; }
    public int YesterdayOrders { get; set; }
    public int TodayNewUsers { get; set; }
    public int YesterdayNewUsers { get; set; }
    public int ActiveSellers { get; set; }
    public int ActiveProducts { get; set; }
    public int CategoryCount { get; set; }
    public int PendingSellerApplications { get; set; }
    public string Currency { get; set; } = "TRY";
}

public class AdminDashboardRevenueTrendPointDto : IDto
{
    public string Label { get; set; } = string.Empty;
    public DateOnly Date { get; set; }
    public decimal Revenue { get; set; }
    public decimal PreviousRevenue { get; set; }
    public int Orders { get; set; }
}

public class AdminDashboardCategorySalesItemDto : IDto
{
    public string CategoryName { get; set; } = string.Empty;
    public int SalesCount { get; set; }
}

public class AdminDashboardUserRegistrationPointDto : IDto
{
    public string Label { get; set; } = string.Empty;
    public DateOnly Date { get; set; }
    public int Count { get; set; }
}

public class AdminDashboardOrderStatusDistributionItemDto : IDto
{
    public OrderStatus Status { get; set; }
    public int Count { get; set; }
}

public class AdminDashboardLowStockItemDto : IDto
{
    public int ProductId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Stock { get; set; }
    public string SellerName { get; set; } = string.Empty;
}

public class AdminDashboardRecentOrderDto : IDto
{
    public int OrderId { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public string Currency { get; set; } = "TRY";
    public OrderStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
}
