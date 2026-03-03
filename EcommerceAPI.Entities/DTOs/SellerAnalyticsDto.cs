using EcommerceAPI.Core.Entities;
using EcommerceAPI.Entities.Enums;

namespace EcommerceAPI.Entities.DTOs;

public class SellerAnalyticsSummaryDto : IDto
{
    public int TotalProducts { get; set; }
    public int ActiveProducts { get; set; }
    public long TotalViews { get; set; }
    public int TotalWishlistCount { get; set; }
    public decimal FavoriteRate { get; set; }
    public decimal ConversionRate { get; set; }
    public decimal AverageRating { get; set; }
    public int ReviewCount { get; set; }
    public decimal ReturnRate { get; set; }
    public int SuccessfulOrderCount { get; set; }
    public int ReturnedRequestCount { get; set; }
    public decimal GrossRevenue { get; set; }
    public string Currency { get; set; } = "TRY";
}

public class SellerDashboardKpiDto : IDto
{
    public int PeriodDays { get; set; } = 30;
    public decimal Revenue { get; set; }
    public decimal RevenueDelta { get; set; }
    public int TotalOrders { get; set; }
    public int CompletedOrdersInPeriod { get; set; }
    public decimal AverageRating { get; set; }
    public int ReviewCount { get; set; }
    public decimal NetEarnings { get; set; }
    public decimal CommissionRate { get; set; }
    public string Currency { get; set; } = "TRY";
}

public class SellerDashboardRevenueTrendPointDto : IDto
{
    public string Label { get; set; } = string.Empty;
    public DateOnly Date { get; set; }
    public decimal Revenue { get; set; }
    public int Orders { get; set; }
}

public class SellerDashboardOrderStatusDistributionItemDto : IDto
{
    public OrderStatus Status { get; set; }
    public int Count { get; set; }
}

public class SellerDashboardProductPerformanceItemDto : IDto
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public int UnitsSold { get; set; }
    public decimal Revenue { get; set; }
    public decimal AverageRating { get; set; }
    public int StockQuantity { get; set; }
    public string Currency { get; set; } = "TRY";
}

public class SellerDashboardRecentOrderDto : IDto
{
    public int OrderId { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public string Currency { get; set; } = "TRY";
    public OrderStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class SellerAnalyticsTrendPointDto : IDto
{
    public DateOnly Date { get; set; }
    public long Views { get; set; }
    public int Favorites { get; set; }
    public int Orders { get; set; }
    public decimal Revenue { get; set; }
    public decimal AverageRating { get; set; }
}

public class SellerFinanceSummaryDto : IDto
{
    public int PeriodDays { get; set; }
    public int TotalOrders { get; set; }
    public decimal GrossSales { get; set; }
    public decimal RefundedAmount { get; set; }
    public decimal NetSales { get; set; }
    public decimal CommissionRate { get; set; }
    public decimal CommissionAmount { get; set; }
    public decimal NetEarnings { get; set; }
    public decimal AverageOrderValue { get; set; }
    public decimal AverageDailyRevenue { get; set; }
    public decimal LifetimeGrossRevenue { get; set; }
    public string Currency { get; set; } = "TRY";
    public List<SellerFinanceTrendPointDto> DailyTrend { get; set; } = new();
    public List<SellerFinanceMonthlySummaryDto> MonthlySummaries { get; set; } = new();
}

public class SellerFinanceTrendPointDto : IDto
{
    public DateOnly Date { get; set; }
    public int Orders { get; set; }
    public decimal GrossSales { get; set; }
    public decimal NetSales { get; set; }
    public decimal CommissionAmount { get; set; }
    public decimal NetEarnings { get; set; }
}

public class SellerFinanceMonthlySummaryDto : IDto
{
    public string MonthKey { get; set; } = string.Empty;
    public string MonthLabel { get; set; } = string.Empty;
    public int Orders { get; set; }
    public decimal GrossSales { get; set; }
    public decimal NetSales { get; set; }
    public decimal CommissionAmount { get; set; }
    public decimal NetEarnings { get; set; }
}
