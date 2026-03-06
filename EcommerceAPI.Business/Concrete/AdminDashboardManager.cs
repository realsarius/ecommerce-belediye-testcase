using System.Globalization;
using EcommerceAPI.Business.Abstract;
using EcommerceAPI.Core.Utilities.Results;
using EcommerceAPI.DataAccess.Abstract;
using EcommerceAPI.Entities.DTOs;
using EcommerceAPI.Entities.Enums;

namespace EcommerceAPI.Business.Concrete;

public class AdminDashboardManager : IAdminDashboardService
{
    private static readonly CultureInfo DashboardCulture = ResolveDashboardCulture();

    private readonly IOrderDal _orderDal;
    private readonly IUserDal _userDal;
    private readonly IProductDal _productDal;
    private readonly ICategoryDal _categoryDal;
    private readonly ISellerProfileDal _sellerProfileDal;

    public AdminDashboardManager(
        IOrderDal orderDal,
        IUserDal userDal,
        IProductDal productDal,
        ICategoryDal categoryDal,
        ISellerProfileDal sellerProfileDal)
    {
        _orderDal = orderDal;
        _userDal = userDal;
        _productDal = productDal;
        _categoryDal = categoryDal;
        _sellerProfileDal = sellerProfileDal;
    }

    public async Task<IDataResult<AdminDashboardKpiDto>> GetKpiAsync()
    {
        var orders = await _orderDal.GetAdminDashboardOrderProjectionsAsync();
        var userCreatedDates = await _userDal.GetAdminDashboardUserCreatedDatesAsync();
        var productSummary = await _productDal.GetAdminDashboardProductSummaryAsync();
        var categoryCount = await _categoryDal.GetDashboardCategoryCountAsync();
        var pendingSellerApplications = await _sellerProfileDal.GetPendingApplicationCountAsync();

        var today = DateTime.UtcNow.Date;
        var yesterday = today.AddDays(-1);

        var todayOrders = orders.Where(order => order.CreatedAt.Date == today).ToList();
        var yesterdayOrders = orders.Where(order => order.CreatedAt.Date == yesterday).ToList();
        var todayUsers = userCreatedDates.Count(createdAt => createdAt.Date == today);
        var yesterdayUsers = userCreatedDates.Count(createdAt => createdAt.Date == yesterday);

        var kpi = new AdminDashboardKpiDto
        {
            TodayRevenue = Math.Round(CalculateRevenue(todayOrders), 2),
            YesterdayRevenue = Math.Round(CalculateRevenue(yesterdayOrders), 2),
            TodayOrders = todayOrders.Count,
            YesterdayOrders = yesterdayOrders.Count,
            TodayNewUsers = todayUsers,
            YesterdayNewUsers = yesterdayUsers,
            ActiveSellers = productSummary.ActiveSellers,
            ActiveProducts = productSummary.ActiveProducts,
            CategoryCount = categoryCount,
            PendingSellerApplications = pendingSellerApplications,
            Currency = productSummary.Currency
        };

        return new SuccessDataResult<AdminDashboardKpiDto>(kpi);
    }

    public async Task<IDataResult<List<AdminDashboardRevenueTrendPointDto>>> GetRevenueTrendAsync(string period = "daily")
    {
        var normalizedPeriod = string.IsNullOrWhiteSpace(period)
            ? "daily"
            : period.Trim().ToLowerInvariant();

        if (normalizedPeriod is not ("daily" or "weekly" or "monthly"))
        {
            return new ErrorDataResult<List<AdminDashboardRevenueTrendPointDto>>("Geçersiz trend periyodu.");
        }

        var orders = await _orderDal.GetAdminDashboardOrderProjectionsAsync();
        var trend = normalizedPeriod switch
        {
            "weekly" => BuildWeeklyTrend(orders),
            "monthly" => BuildMonthlyTrend(orders),
            _ => BuildDailyTrend(orders)
        };

        return new SuccessDataResult<List<AdminDashboardRevenueTrendPointDto>>(trend);
    }

    public async Task<IDataResult<List<AdminDashboardCategorySalesItemDto>>> GetCategorySalesAsync()
    {
        var categorySales = (await _orderDal.GetAdminDashboardCategorySalesAsync()).ToList();

        return new SuccessDataResult<List<AdminDashboardCategorySalesItemDto>>(categorySales);
    }

    public async Task<IDataResult<List<AdminDashboardUserRegistrationPointDto>>> GetUserRegistrationsAsync(int days = 30)
    {
        var normalizedDays = Math.Clamp(days, 7, 90);
        var userCreatedDates = await _userDal.GetAdminDashboardUserCreatedDatesAsync();
        var today = DateTime.UtcNow.Date;

        var result = Enumerable.Range(0, normalizedDays)
            .Select(offset => today.AddDays(-(normalizedDays - 1 - offset)))
            .Select(date => new AdminDashboardUserRegistrationPointDto
            {
                Date = DateOnly.FromDateTime(date),
                Label = date.ToString("dd MMM", DashboardCulture),
                Count = userCreatedDates.Count(createdAt => createdAt.Date == date)
            })
            .ToList();

        return new SuccessDataResult<List<AdminDashboardUserRegistrationPointDto>>(result);
    }

    public async Task<IDataResult<List<AdminDashboardOrderStatusDistributionItemDto>>> GetOrderStatusDistributionAsync()
    {
        var orders = await _orderDal.GetAdminDashboardOrderProjectionsAsync();
        var distribution = Enum.GetValues<OrderStatus>()
            .Select(status => new AdminDashboardOrderStatusDistributionItemDto
            {
                Status = status,
                Count = orders.Count(order => order.Status == status)
            })
            .ToList();

        return new SuccessDataResult<List<AdminDashboardOrderStatusDistributionItemDto>>(distribution);
    }

    public async Task<IDataResult<List<AdminDashboardLowStockItemDto>>> GetLowStockAsync(int threshold = 5)
    {
        var normalizedThreshold = Math.Clamp(threshold, 0, 50);
        var result = (await _productDal.GetAdminDashboardLowStockAsync(normalizedThreshold)).ToList();

        return new SuccessDataResult<List<AdminDashboardLowStockItemDto>>(result);
    }

    public async Task<IDataResult<List<AdminDashboardRecentOrderDto>>> GetRecentOrdersAsync(int limit = 5)
    {
        var normalizedLimit = Math.Clamp(limit, 3, 20);
        var orders = (await _orderDal.GetAdminDashboardOrderProjectionsAsync())
            .OrderByDescending(order => order.CreatedAt)
            .Take(normalizedLimit)
            .Select(order => new AdminDashboardRecentOrderDto
            {
                OrderId = order.OrderId,
                OrderNumber = order.OrderNumber,
                CustomerName = order.CustomerName,
                TotalAmount = Math.Round(order.TotalAmount, 2),
                Currency = order.Currency,
                Status = order.Status,
                CreatedAt = order.CreatedAt
            })
            .ToList();

        return new SuccessDataResult<List<AdminDashboardRecentOrderDto>>(orders);
    }

    private static List<AdminDashboardRevenueTrendPointDto> BuildDailyTrend(IEnumerable<AdminDashboardOrderProjectionDto> orders)
    {
        var today = DateTime.UtcNow.Date;

        return Enumerable.Range(0, 7)
            .Select(offset =>
            {
                var date = today.AddDays(-(6 - offset));
                var previousDate = date.AddDays(-7);
                var currentOrders = orders.Where(order => order.CreatedAt.Date == date).ToList();
                var previousOrders = orders.Where(order => order.CreatedAt.Date == previousDate).ToList();

                return new AdminDashboardRevenueTrendPointDto
                {
                    Label = date.ToString("dd MMM", DashboardCulture),
                    Date = DateOnly.FromDateTime(date),
                    Revenue = Math.Round(CalculateRevenue(currentOrders), 2),
                    PreviousRevenue = Math.Round(CalculateRevenue(previousOrders), 2),
                    Orders = currentOrders.Count
                };
            })
            .ToList();
    }

    private static List<AdminDashboardRevenueTrendPointDto> BuildWeeklyTrend(IEnumerable<AdminDashboardOrderProjectionDto> orders)
    {
        var currentWeekStart = StartOfWeek(DateTime.UtcNow.Date);

        return Enumerable.Range(0, 8)
            .Select(offset =>
            {
                var currentStart = currentWeekStart.AddDays(-(7 * (7 - offset)));
                var currentEnd = currentStart.AddDays(7);
                var previousStart = currentStart.AddDays(-56);
                var previousEnd = previousStart.AddDays(7);

                var currentOrders = orders.Where(order => order.CreatedAt.Date >= currentStart && order.CreatedAt.Date < currentEnd).ToList();
                var previousOrders = orders.Where(order => order.CreatedAt.Date >= previousStart && order.CreatedAt.Date < previousEnd).ToList();

                return new AdminDashboardRevenueTrendPointDto
                {
                    Label = currentStart.ToString("dd MMM", DashboardCulture),
                    Date = DateOnly.FromDateTime(currentStart),
                    Revenue = Math.Round(CalculateRevenue(currentOrders), 2),
                    PreviousRevenue = Math.Round(CalculateRevenue(previousOrders), 2),
                    Orders = currentOrders.Count
                };
            })
            .ToList();
    }

    private static List<AdminDashboardRevenueTrendPointDto> BuildMonthlyTrend(IEnumerable<AdminDashboardOrderProjectionDto> orders)
    {
        var today = DateTime.UtcNow.Date;
        var currentMonthStart = new DateTime(today.Year, today.Month, 1);

        return Enumerable.Range(0, 6)
            .Select(offset =>
            {
                var currentStart = currentMonthStart.AddMonths(-(5 - offset));
                var currentEnd = currentStart.AddMonths(1);
                var previousStart = currentStart.AddMonths(-6);
                var previousEnd = previousStart.AddMonths(1);

                var currentOrders = orders.Where(order => order.CreatedAt.Date >= currentStart && order.CreatedAt.Date < currentEnd).ToList();
                var previousOrders = orders.Where(order => order.CreatedAt.Date >= previousStart && order.CreatedAt.Date < previousEnd).ToList();

                return new AdminDashboardRevenueTrendPointDto
                {
                    Label = currentStart.ToString("MMM yyyy", DashboardCulture),
                    Date = DateOnly.FromDateTime(currentStart),
                    Revenue = Math.Round(CalculateRevenue(currentOrders), 2),
                    PreviousRevenue = Math.Round(CalculateRevenue(previousOrders), 2),
                    Orders = currentOrders.Count
                };
            })
            .ToList();
    }

    private static decimal CalculateRevenue(IEnumerable<AdminDashboardOrderProjectionDto> orders)
    {
        return orders
            .Where(IsRevenueOrder)
            .Sum(order => order.TotalAmount);
    }

    private static bool IsRevenueOrder(AdminDashboardOrderProjectionDto order)
    {
        return order.Status is OrderStatus.Paid or OrderStatus.Processing or OrderStatus.Shipped or OrderStatus.Delivered;
    }

    private static CultureInfo ResolveDashboardCulture()
    {
        try
        {
            return CultureInfo.GetCultureInfo("tr-TR");
        }
        catch (CultureNotFoundException)
        {
            return CultureInfo.InvariantCulture;
        }
    }

    private static DateTime StartOfWeek(DateTime date)
    {
        var diff = (7 + (date.DayOfWeek - DayOfWeek.Monday)) % 7;
        return date.AddDays(-1 * diff).Date;
    }
}
