using System.Globalization;
using EcommerceAPI.Business.Abstract;
using EcommerceAPI.Core.Utilities.Results;
using EcommerceAPI.DataAccess.Abstract;
using EcommerceAPI.Entities.Concrete;
using EcommerceAPI.Entities.DTOs;
using EcommerceAPI.Entities.Enums;

namespace EcommerceAPI.Business.Concrete;

public class AdminDashboardManager : IAdminDashboardService
{
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
        var orders = (await _orderDal.GetAllOrdersWithDetailsAsync()).ToList();
        var users = await _userDal.GetAdminUsersWithDetailsAsync();
        var products = await _productDal.GetAllActiveWithDetailsAsync();
        var categories = await _categoryDal.GetAllWithProductsAsync();
        var sellerProfiles = await _sellerProfileDal.GetAdminListWithDetailsAsync();

        var today = DateTime.UtcNow.Date;
        var yesterday = today.AddDays(-1);

        var todayOrders = orders.Where(order => order.CreatedAt.Date == today).ToList();
        var yesterdayOrders = orders.Where(order => order.CreatedAt.Date == yesterday).ToList();
        var todayUsers = users.Where(user => user.CreatedAt.Date == today).ToList();
        var yesterdayUsers = users.Where(user => user.CreatedAt.Date == yesterday).ToList();

        var kpi = new AdminDashboardKpiDto
        {
            TodayRevenue = Math.Round(CalculateRevenue(todayOrders), 2),
            YesterdayRevenue = Math.Round(CalculateRevenue(yesterdayOrders), 2),
            TodayOrders = todayOrders.Count,
            YesterdayOrders = yesterdayOrders.Count,
            TodayNewUsers = todayUsers.Count,
            YesterdayNewUsers = yesterdayUsers.Count,
            ActiveSellers = products
                .Where(product => product.IsActive && product.SellerId.HasValue)
                .Select(product => product.SellerId!.Value)
                .Distinct()
                .Count(),
            ActiveProducts = products.Count(product => product.IsActive),
            CategoryCount = categories.Count,
            PendingSellerApplications = sellerProfiles.Count(profile => !profile.IsVerified && profile.User.AccountStatus == UserAccountStatus.Active),
            Currency = products.FirstOrDefault()?.Currency ?? "TRY"
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

        var orders = (await _orderDal.GetAllOrdersWithDetailsAsync()).ToList();
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
        var orders = (await _orderDal.GetAllOrdersWithDetailsAsync()).ToList();
        var categorySales = orders
            .Where(IsRevenueOrder)
            .SelectMany(order => order.OrderItems)
            .Where(item => item.Product?.Category != null)
            .GroupBy(item => item.Product.Category.Name)
            .Select(group => new AdminDashboardCategorySalesItemDto
            {
                CategoryName = group.Key,
                SalesCount = group.Sum(item => item.Quantity)
            })
            .OrderByDescending(item => item.SalesCount)
            .Take(6)
            .ToList();

        return new SuccessDataResult<List<AdminDashboardCategorySalesItemDto>>(categorySales);
    }

    public async Task<IDataResult<List<AdminDashboardUserRegistrationPointDto>>> GetUserRegistrationsAsync(int days = 30)
    {
        var normalizedDays = Math.Clamp(days, 7, 90);
        var users = await _userDal.GetAdminUsersWithDetailsAsync();
        var today = DateTime.UtcNow.Date;
        var culture = CultureInfo.GetCultureInfo("tr-TR");

        var result = Enumerable.Range(0, normalizedDays)
            .Select(offset => today.AddDays(-(normalizedDays - 1 - offset)))
            .Select(date => new AdminDashboardUserRegistrationPointDto
            {
                Date = DateOnly.FromDateTime(date),
                Label = date.ToString("dd MMM", culture),
                Count = users.Count(user => user.CreatedAt.Date == date)
            })
            .ToList();

        return new SuccessDataResult<List<AdminDashboardUserRegistrationPointDto>>(result);
    }

    public async Task<IDataResult<List<AdminDashboardOrderStatusDistributionItemDto>>> GetOrderStatusDistributionAsync()
    {
        var orders = await _orderDal.GetAllOrdersWithDetailsAsync();
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
        var products = await _productDal.GetAllActiveWithDetailsAsync();

        var result = products
            .Where(product => (product.Inventory?.QuantityAvailable ?? 0) <= normalizedThreshold)
            .OrderBy(product => product.Inventory?.QuantityAvailable ?? 0)
            .ThenBy(product => product.Name)
            .Take(5)
            .Select(product => new AdminDashboardLowStockItemDto
            {
                ProductId = product.Id,
                Name = product.Name,
                Stock = product.Inventory?.QuantityAvailable ?? 0,
                SellerName = product.Seller?.BrandName ?? "Satıcı bilgisi yok"
            })
            .ToList();

        return new SuccessDataResult<List<AdminDashboardLowStockItemDto>>(result);
    }

    public async Task<IDataResult<List<AdminDashboardRecentOrderDto>>> GetRecentOrdersAsync(int limit = 5)
    {
        var normalizedLimit = Math.Clamp(limit, 3, 20);
        var orders = (await _orderDal.GetAllOrdersWithDetailsAsync())
            .OrderByDescending(order => order.CreatedAt)
            .Take(normalizedLimit)
            .Select(order => new AdminDashboardRecentOrderDto
            {
                OrderId = order.Id,
                OrderNumber = order.OrderNumber,
                CustomerName = order.User == null
                    ? string.Empty
                    : $"{order.User.FirstName} {order.User.LastName}".Trim(),
                TotalAmount = Math.Round(order.TotalAmount, 2),
                Currency = order.Currency,
                Status = order.Status,
                CreatedAt = order.CreatedAt
            })
            .ToList();

        return new SuccessDataResult<List<AdminDashboardRecentOrderDto>>(orders);
    }

    private static List<AdminDashboardRevenueTrendPointDto> BuildDailyTrend(IEnumerable<Order> orders)
    {
        var culture = CultureInfo.GetCultureInfo("tr-TR");
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
                    Label = date.ToString("dd MMM", culture),
                    Date = DateOnly.FromDateTime(date),
                    Revenue = Math.Round(CalculateRevenue(currentOrders), 2),
                    PreviousRevenue = Math.Round(CalculateRevenue(previousOrders), 2),
                    Orders = currentOrders.Count
                };
            })
            .ToList();
    }

    private static List<AdminDashboardRevenueTrendPointDto> BuildWeeklyTrend(IEnumerable<Order> orders)
    {
        var culture = CultureInfo.GetCultureInfo("tr-TR");
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
                    Label = currentStart.ToString("dd MMM", culture),
                    Date = DateOnly.FromDateTime(currentStart),
                    Revenue = Math.Round(CalculateRevenue(currentOrders), 2),
                    PreviousRevenue = Math.Round(CalculateRevenue(previousOrders), 2),
                    Orders = currentOrders.Count
                };
            })
            .ToList();
    }

    private static List<AdminDashboardRevenueTrendPointDto> BuildMonthlyTrend(IEnumerable<Order> orders)
    {
        var culture = CultureInfo.GetCultureInfo("tr-TR");
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
                    Label = currentStart.ToString("MMM yyyy", culture),
                    Date = DateOnly.FromDateTime(currentStart),
                    Revenue = Math.Round(CalculateRevenue(currentOrders), 2),
                    PreviousRevenue = Math.Round(CalculateRevenue(previousOrders), 2),
                    Orders = currentOrders.Count
                };
            })
            .ToList();
    }

    private static decimal CalculateRevenue(IEnumerable<Order> orders)
    {
        return orders
            .Where(IsRevenueOrder)
            .Sum(order => order.TotalAmount);
    }

    private static bool IsRevenueOrder(Order order)
    {
        return order.Status is OrderStatus.Paid or OrderStatus.Processing or OrderStatus.Shipped or OrderStatus.Delivered;
    }

    private static DateTime StartOfWeek(DateTime date)
    {
        var diff = (7 + (date.DayOfWeek - DayOfWeek.Monday)) % 7;
        return date.AddDays(-1 * diff).Date;
    }
}
