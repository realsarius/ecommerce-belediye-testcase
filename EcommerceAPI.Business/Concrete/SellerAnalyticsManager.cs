using System.Globalization;
using EcommerceAPI.Application.Abstractions.ServiceContracts;
using EcommerceAPI.Core.Interfaces;
using EcommerceAPI.Core.Utilities.Results;
using EcommerceAPI.Application.Abstractions.Persistence;
using EcommerceAPI.Entities.Concrete;
using EcommerceAPI.Entities.DTOs;
using EcommerceAPI.Entities.Enums;

namespace EcommerceAPI.Business.Concrete;

public class SellerAnalyticsManager : ISellerAnalyticsService
{
    private const decimal DefaultCommissionRate = 0.10m;
    private static readonly CultureInfo AnalyticsCulture = ResolveAnalyticsCulture();

    private readonly IProductDal _productDal;
    private readonly IOrderDal _orderDal;
    private readonly IReturnRequestDal _returnRequestDal;
    private readonly IProductReviewDal _productReviewDal;
    private readonly ISellerProfileDal _sellerProfileDal;
    private readonly IWishlistItemDal _wishlistItemDal;
    private readonly IRecommendationCacheService _recommendationCacheService;

    public SellerAnalyticsManager(
        IProductDal productDal,
        IOrderDal orderDal,
        IReturnRequestDal returnRequestDal,
        IProductReviewDal productReviewDal,
        ISellerProfileDal sellerProfileDal,
        IWishlistItemDal wishlistItemDal,
        IRecommendationCacheService recommendationCacheService)
    {
        _productDal = productDal;
        _orderDal = orderDal;
        _returnRequestDal = returnRequestDal;
        _productReviewDal = productReviewDal;
        _sellerProfileDal = sellerProfileDal;
        _wishlistItemDal = wishlistItemDal;
        _recommendationCacheService = recommendationCacheService;
    }

    public async Task<IDataResult<SellerAnalyticsSummaryDto>> GetSummaryAsync(int sellerId)
    {
        var products = await _productDal.GetListAsync(product => product.SellerId == sellerId);
        var productList = products.ToList();
        var productIds = productList.Select(product => product.Id).ToArray();

        if (productIds.Length == 0)
        {
            return new SuccessDataResult<SellerAnalyticsSummaryDto>(new SellerAnalyticsSummaryDto());
        }

        var orders = await _orderDal.GetOrdersBySellerIdAsync(sellerId);
        var reviews = await _productReviewDal.GetListAsync(review => productIds.Contains(review.ProductId));
        var returnRequests = await _returnRequestDal.GetBySellerIdAsync(sellerId);
        var viewCounts = await _recommendationCacheService.GetProductViewCountsAsync(productIds);

        var totalViews = viewCounts.Values.Sum();
        var totalWishlistCount = productList.Sum(product => product.WishlistCount);
        var successfulOrders = orders
            .Where(order => order.Payment?.Status == PaymentStatus.Success && order.Status != OrderStatus.Cancelled)
            .ToList();
        var successfulSellerItems = successfulOrders
            .SelectMany(order => order.OrderItems.Where(item => item.Product.SellerId == sellerId))
            .ToList();
        var grossRevenue = successfulSellerItems.Sum(item => item.PriceSnapshot * item.Quantity);
        var returnedRequests = returnRequests.Count(request =>
            request.Status is ReturnRequestStatus.Approved or ReturnRequestStatus.RefundPending or ReturnRequestStatus.Refunded);
        var reviewList = reviews.ToList();

        var summary = new SellerAnalyticsSummaryDto
        {
            TotalProducts = productList.Count,
            ActiveProducts = productList.Count(product => product.IsActive),
            TotalViews = totalViews,
            TotalWishlistCount = totalWishlistCount,
            FavoriteRate = CalculateRatio(totalWishlistCount, totalViews),
            ConversionRate = CalculateRatio(successfulSellerItems.Sum(item => item.Quantity), totalViews),
            AverageRating = reviewList.Count == 0 ? 0 : Math.Round((decimal)reviewList.Average(review => review.Rating), 2),
            ReviewCount = reviewList.Count,
            ReturnRate = CalculateRatio(returnedRequests, successfulOrders.Count),
            SuccessfulOrderCount = successfulOrders.Count,
            ReturnedRequestCount = returnedRequests,
            GrossRevenue = Math.Round(grossRevenue, 2),
            Currency = productList.FirstOrDefault()?.Currency ?? "TRY"
        };

        return new SuccessDataResult<SellerAnalyticsSummaryDto>(summary);
    }

    public async Task<IDataResult<List<SellerAnalyticsTrendPointDto>>> GetTrendAsync(int sellerId, int days = 30)
    {
        var normalizedDays = Math.Clamp(days, 7, 90);
        var fromDate = DateTime.UtcNow.Date.AddDays(-(normalizedDays - 1));
        var fromDateOnly = DateOnly.FromDateTime(fromDate);

        var products = await _productDal.GetListAsync(product => product.SellerId == sellerId);
        var productIds = products.Select(product => product.Id).ToArray();

        if (productIds.Length == 0)
        {
            return new SuccessDataResult<List<SellerAnalyticsTrendPointDto>>(CreateEmptyTrend(fromDateOnly, normalizedDays));
        }

        var viewTrend = await _recommendationCacheService.GetProductViewTrendAsync(productIds, normalizedDays);
        var wishlistItems = await _wishlistItemDal.GetListAsync(item => productIds.Contains(item.ProductId) && item.AddedAt >= fromDate);
        var reviews = await _productReviewDal.GetListAsync(review => productIds.Contains(review.ProductId) && review.CreatedAt >= fromDate);
        var orders = (await _orderDal.GetOrdersBySellerIdAsync(sellerId))
            .Where(order => order.CreatedAt >= fromDate && order.Payment?.Status == PaymentStatus.Success && order.Status != OrderStatus.Cancelled)
            .ToList();

        var trend = CreateEmptyTrend(fromDateOnly, normalizedDays)
            .ToDictionary(point => point.Date, point => point);

        foreach (var point in trend.Values)
        {
            if (viewTrend.TryGetValue(point.Date, out var views))
            {
                point.Views = views;
            }
        }

        foreach (var wishlistItem in wishlistItems)
        {
            var date = DateOnly.FromDateTime(wishlistItem.AddedAt);
            if (trend.TryGetValue(date, out var point))
            {
                point.Favorites += 1;
            }
        }

        foreach (var order in orders)
        {
            var date = DateOnly.FromDateTime(order.CreatedAt);
            if (!trend.TryGetValue(date, out var point))
            {
                continue;
            }

            var sellerItems = order.OrderItems.Where(item => item.Product.SellerId == sellerId).ToList();
            if (sellerItems.Count == 0)
            {
                continue;
            }

            point.Orders += 1;
            point.Revenue += sellerItems.Sum(item => item.PriceSnapshot * item.Quantity);
        }

        var ratingBuckets = reviews
            .GroupBy(review => DateOnly.FromDateTime(review.CreatedAt))
            .ToDictionary(group => group.Key, group => Math.Round((decimal)group.Average(review => review.Rating), 2));

        foreach (var (date, rating) in ratingBuckets)
        {
            if (trend.TryGetValue(date, out var point))
            {
                point.AverageRating = rating;
            }
        }

        return new SuccessDataResult<List<SellerAnalyticsTrendPointDto>>(trend.Values.OrderBy(point => point.Date).ToList());
    }

    public async Task<IDataResult<SellerFinanceSummaryDto>> GetFinanceSummaryAsync(int sellerId, int days = 30, DateOnly? from = null, DateOnly? to = null)
    {
        var rangeResult = ResolveFinanceDateRange(days, from, to);
        if (!rangeResult.Success)
        {
            return new ErrorDataResult<SellerFinanceSummaryDto>(rangeResult.Message ?? "Geçersiz tarih aralığı.");
        }

        var normalizedDays = rangeResult.Data.PeriodDays;
        var rangeStart = rangeResult.Data.FromDate;
        var rangeEnd = rangeResult.Data.ToDate;
        var fromDate = rangeStart.ToDateTime(TimeOnly.MinValue);
        var toDateExclusive = rangeEnd.AddDays(1).ToDateTime(TimeOnly.MinValue);
        var commissionRatePercent = await GetCommissionRatePercentAsync(sellerId);
        var commissionRate = commissionRatePercent / 100m;

        var products = await _productDal.GetListAsync(product => product.SellerId == sellerId);
        var productList = products.ToList();
        var currency = productList.FirstOrDefault()?.Currency ?? "TRY";

        var orders = (await _orderDal.GetOrdersBySellerIdAsync(sellerId))
            .Where(order => order.Payment?.Status == PaymentStatus.Success)
            .ToList();

        if (orders.Count == 0)
        {
            return new SuccessDataResult<SellerFinanceSummaryDto>(new SellerFinanceSummaryDto
            {
                PeriodDays = normalizedDays,
                FromDate = rangeStart,
                ToDate = rangeEnd,
                CommissionRate = commissionRatePercent,
                Currency = currency
            });
        }

        var lifetimeGrossRevenue = orders
            .Where(order => order.Status != OrderStatus.Cancelled)
            .SelectMany(order => order.OrderItems.Where(item => item.Product.SellerId == sellerId))
            .Sum(item => item.PriceSnapshot * item.Quantity);

        var periodOrders = orders
            .Where(order => order.CreatedAt >= fromDate && order.CreatedAt < toDateExclusive)
            .ToList();

        var dailyTrend = Enumerable.Range(0, normalizedDays)
            .Select(offset =>
            {
                var date = rangeStart.AddDays(offset);
                var sellerItems = periodOrders
                    .Where(order => DateOnly.FromDateTime(order.CreatedAt) == date && order.Status != OrderStatus.Cancelled)
                    .SelectMany(order => order.OrderItems.Where(item => item.Product.SellerId == sellerId))
                    .ToList();

                var refundedItems = periodOrders
                    .Where(order => DateOnly.FromDateTime(order.CreatedAt) == date && order.Status == OrderStatus.Refunded)
                    .SelectMany(order => order.OrderItems.Where(item => item.Product.SellerId == sellerId))
                    .ToList();

                var grossSales = sellerItems.Sum(item => item.PriceSnapshot * item.Quantity);
                var refundedAmount = refundedItems.Sum(item => item.PriceSnapshot * item.Quantity);
                var netSales = Math.Max(0, grossSales - refundedAmount);
                var commissionAmount = Math.Round(netSales * commissionRate, 2);

                return new SellerFinanceTrendPointDto
                {
                    Date = date,
                    Orders = periodOrders.Count(order =>
                        DateOnly.FromDateTime(order.CreatedAt) == date &&
                        order.Status != OrderStatus.Cancelled &&
                        order.OrderItems.Any(item => item.Product.SellerId == sellerId)),
                    GrossSales = Math.Round(grossSales, 2),
                    NetSales = Math.Round(netSales, 2),
                    CommissionAmount = commissionAmount,
                    NetEarnings = Math.Round(netSales - commissionAmount, 2)
                };
            })
            .ToList();

        var grossSalesTotal = dailyTrend.Sum(point => point.GrossSales);
        var netSalesTotal = dailyTrend.Sum(point => point.NetSales);
        var commissionAmountTotal = dailyTrend.Sum(point => point.CommissionAmount);
        var refundedAmountTotal = Math.Round(grossSalesTotal - netSalesTotal, 2);
        var totalOrders = dailyTrend.Sum(point => point.Orders);

        var monthlySummaries = dailyTrend
            .GroupBy(point => new { point.Date.Year, point.Date.Month })
            .Select(group =>
            {
                var representativeDate = new DateTime(group.Key.Year, group.Key.Month, 1);
                return new SellerFinanceMonthlySummaryDto
                {
                    MonthKey = $"{group.Key.Year}-{group.Key.Month:00}",
                    MonthLabel = representativeDate.ToString("MMMM yyyy", AnalyticsCulture),
                    Orders = group.Sum(point => point.Orders),
                    GrossSales = Math.Round(group.Sum(point => point.GrossSales), 2),
                    NetSales = Math.Round(group.Sum(point => point.NetSales), 2),
                    CommissionAmount = Math.Round(group.Sum(point => point.CommissionAmount), 2),
                    NetEarnings = Math.Round(group.Sum(point => point.NetEarnings), 2),
                };
            })
            .OrderBy(summary => summary.MonthKey)
            .ToList();

        var summary = new SellerFinanceSummaryDto
        {
            PeriodDays = normalizedDays,
            FromDate = rangeStart,
            ToDate = rangeEnd,
            TotalOrders = totalOrders,
            GrossSales = Math.Round(grossSalesTotal, 2),
            RefundedAmount = refundedAmountTotal,
            NetSales = Math.Round(netSalesTotal, 2),
            CommissionRate = commissionRatePercent,
            CommissionAmount = Math.Round(commissionAmountTotal, 2),
            NetEarnings = Math.Round(netSalesTotal - commissionAmountTotal, 2),
            AverageOrderValue = totalOrders == 0 ? 0 : Math.Round(grossSalesTotal / totalOrders, 2),
            AverageDailyRevenue = dailyTrend.Count == 0 ? 0 : Math.Round(grossSalesTotal / dailyTrend.Count, 2),
            LifetimeGrossRevenue = Math.Round(lifetimeGrossRevenue, 2),
            Currency = currency,
            DailyTrend = dailyTrend,
            MonthlySummaries = monthlySummaries
        };

        return new SuccessDataResult<SellerFinanceSummaryDto>(summary);
    }

    private static IDataResult<(DateOnly FromDate, DateOnly ToDate, int PeriodDays)> ResolveFinanceDateRange(int days, DateOnly? from, DateOnly? to)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        if (from.HasValue || to.HasValue)
        {
            var resolvedTo = to ?? today;
            var resolvedFrom = from ?? resolvedTo.AddDays(-(Math.Clamp(days, 7, 90) - 1));

            if (resolvedFrom > resolvedTo)
            {
                return new ErrorDataResult<(DateOnly FromDate, DateOnly ToDate, int PeriodDays)>("Başlangıç tarihi bitiş tarihinden büyük olamaz.");
            }

            var periodDays = resolvedTo.DayNumber - resolvedFrom.DayNumber + 1;
            if (periodDays > 180)
            {
                return new ErrorDataResult<(DateOnly FromDate, DateOnly ToDate, int PeriodDays)>("Tarih aralığı en fazla 180 gün olabilir.");
            }

            return new SuccessDataResult<(DateOnly FromDate, DateOnly ToDate, int PeriodDays)>((resolvedFrom, resolvedTo, periodDays));
        }

        var normalizedDays = Math.Clamp(days, 7, 90);
        var defaultTo = today;
        var defaultFrom = defaultTo.AddDays(-(normalizedDays - 1));
        return new SuccessDataResult<(DateOnly FromDate, DateOnly ToDate, int PeriodDays)>((defaultFrom, defaultTo, normalizedDays));
    }

    public async Task<IDataResult<SellerDashboardKpiDto>> GetDashboardKpiAsync(int sellerId, int days = 30)
    {
        var normalizedDays = Math.Clamp(days, 7, 90);
        var currentPeriodStart = DateTime.UtcNow.Date.AddDays(-(normalizedDays - 1));
        var previousPeriodStart = currentPeriodStart.AddDays(-normalizedDays);
        var commissionRatePercent = await GetCommissionRatePercentAsync(sellerId);
        var commissionRate = commissionRatePercent / 100m;

        var products = (await _productDal.GetListAsync(product => product.SellerId == sellerId)).ToList();
        var currency = products.FirstOrDefault()?.Currency ?? "TRY";
        var orders = (await _orderDal.GetOrdersBySellerIdAsync(sellerId)).ToList();
        var productIds = products.Select(product => product.Id).ToArray();
        var reviews = productIds.Length == 0
            ? new List<ProductReview>()
            : (await _productReviewDal.GetListAsync(review => productIds.Contains(review.ProductId))).ToList();

        var currentRevenue = CalculateSellerRevenue(
            orders.Where(order => order.CreatedAt >= currentPeriodStart),
            sellerId);
        var previousRevenue = CalculateSellerRevenue(
            orders.Where(order => order.CreatedAt >= previousPeriodStart && order.CreatedAt < currentPeriodStart),
            sellerId);
        var completedOrdersInPeriod = orders.Count(order =>
            order.CreatedAt >= currentPeriodStart &&
            order.Status == OrderStatus.Delivered &&
            order.Payment?.Status == PaymentStatus.Success);

        var kpi = new SellerDashboardKpiDto
        {
            PeriodDays = normalizedDays,
            Revenue = Math.Round(currentRevenue, 2),
            RevenueDelta = CalculateDelta(currentRevenue, previousRevenue),
            TotalOrders = orders.Count,
            CompletedOrdersInPeriod = completedOrdersInPeriod,
            AverageRating = reviews.Count == 0 ? 0 : Math.Round((decimal)reviews.Average(review => review.Rating), 2),
            ReviewCount = reviews.Count,
            NetEarnings = Math.Round(currentRevenue - currentRevenue * commissionRate, 2),
            CommissionRate = commissionRatePercent,
            Currency = currency
        };

        return new SuccessDataResult<SellerDashboardKpiDto>(kpi);
    }

    public async Task<IDataResult<List<SellerDashboardRevenueTrendPointDto>>> GetDashboardRevenueTrendAsync(int sellerId, string period = "daily")
    {
        var normalizedPeriod = string.IsNullOrWhiteSpace(period)
            ? "daily"
            : period.Trim().ToLowerInvariant();

        if (normalizedPeriod is not ("daily" or "weekly" or "monthly"))
        {
            return new ErrorDataResult<List<SellerDashboardRevenueTrendPointDto>>("Gecersiz trend periyodu.");
        }

        var orders = (await _orderDal.GetOrdersBySellerIdAsync(sellerId)).ToList();
        var trend = normalizedPeriod switch
        {
            "weekly" => BuildWeeklyDashboardTrend(orders, sellerId),
            "monthly" => BuildMonthlyDashboardTrend(orders, sellerId),
            _ => BuildDailyDashboardTrend(orders, sellerId)
        };

        return new SuccessDataResult<List<SellerDashboardRevenueTrendPointDto>>(trend);
    }

    public async Task<IDataResult<List<SellerDashboardOrderStatusDistributionItemDto>>> GetDashboardOrderStatusDistributionAsync(int sellerId)
    {
        var orders = await _orderDal.GetOrdersBySellerIdAsync(sellerId);
        var distribution = Enum.GetValues<OrderStatus>()
            .Select(status => new SellerDashboardOrderStatusDistributionItemDto
            {
                Status = status,
                Count = orders.Count(order => order.Status == status)
            })
            .ToList();

        return new SuccessDataResult<List<SellerDashboardOrderStatusDistributionItemDto>>(distribution);
    }

    public async Task<IDataResult<List<SellerDashboardProductPerformanceItemDto>>> GetDashboardProductPerformanceAsync(int sellerId, int take = 5)
    {
        var normalizedTake = Math.Clamp(take, 3, 20);
        var products = (await _productDal.GetPagedForSellerAsync(1, 5000, sellerId)).Items.ToList();
        var orders = (await _orderDal.GetOrdersBySellerIdAsync(sellerId)).ToList();
        var productIds = products.Select(product => product.Id).ToArray();
        var reviewLookup = productIds.Length == 0
            ? new Dictionary<int, decimal>()
            : (await _productReviewDal.GetListAsync(review => productIds.Contains(review.ProductId)))
                .GroupBy(review => review.ProductId)
                .ToDictionary(
                    group => group.Key,
                    group => Math.Round((decimal)group.Average(review => review.Rating), 2));

        var successfulItems = orders
            .Where(order => order.Payment?.Status == PaymentStatus.Success && order.Status != OrderStatus.Cancelled)
            .SelectMany(order => order.OrderItems.Where(item => item.Product.SellerId == sellerId))
            .GroupBy(item => item.ProductId)
            .ToDictionary(
                group => group.Key,
                group => new
                {
                    UnitsSold = group.Sum(item => item.Quantity),
                    Revenue = group.Sum(item => item.PriceSnapshot * item.Quantity)
                });

        var performance = products
            .Where(product => successfulItems.ContainsKey(product.Id))
            .Select(product =>
            {
                var metrics = successfulItems[product.Id];
                return new SellerDashboardProductPerformanceItemDto
                {
                    ProductId = product.Id,
                    ProductName = product.Name,
                    CategoryName = product.Category?.Name ?? string.Empty,
                    UnitsSold = metrics.UnitsSold,
                    Revenue = Math.Round(metrics.Revenue, 2),
                    AverageRating = reviewLookup.TryGetValue(product.Id, out var averageRating) ? averageRating : 0,
                    StockQuantity = product.Inventory?.QuantityAvailable ?? 0,
                    Currency = product.Currency
                };
            })
            .OrderByDescending(item => item.UnitsSold)
            .ThenByDescending(item => item.Revenue)
            .ThenByDescending(item => item.AverageRating)
            .Take(normalizedTake)
            .ToList();

        return new SuccessDataResult<List<SellerDashboardProductPerformanceItemDto>>(performance);
    }

    public async Task<IDataResult<List<SellerDashboardRecentOrderDto>>> GetDashboardRecentOrdersAsync(int sellerId, int take = 5)
    {
        var normalizedTake = Math.Clamp(take, 3, 20);
        var orders = (await _orderDal.GetOrdersBySellerIdAsync(sellerId))
            .OrderByDescending(order => order.CreatedAt)
            .Take(normalizedTake)
            .Select(order => new SellerDashboardRecentOrderDto
            {
                OrderId = order.Id,
                OrderNumber = order.OrderNumber,
                CustomerName = order.User == null
                    ? string.Empty
                    : $"{order.User.FirstName} {order.User.LastName}".Trim(),
                TotalAmount = Math.Round(
                    order.OrderItems
                        .Where(item => item.Product.SellerId == sellerId)
                        .Sum(item => item.PriceSnapshot * item.Quantity),
                    2),
                Currency = order.Currency,
                Status = order.Status,
                CreatedAt = order.CreatedAt
            })
            .ToList();

        return new SuccessDataResult<List<SellerDashboardRecentOrderDto>>(orders);
    }

    private static List<SellerAnalyticsTrendPointDto> CreateEmptyTrend(DateOnly fromDate, int days)
    {
        return Enumerable.Range(0, days)
            .Select(offset => new SellerAnalyticsTrendPointDto
            {
                Date = fromDate.AddDays(offset)
            })
            .ToList();
    }

    private static List<SellerDashboardRevenueTrendPointDto> BuildDailyDashboardTrend(IEnumerable<Order> orders, int sellerId)
    {
        var today = DateTime.UtcNow.Date;

        return Enumerable.Range(0, 30)
            .Select(offset => today.AddDays(-(29 - offset)))
            .Select(date =>
            {
                var matchingOrders = orders
                    .Where(order => order.CreatedAt.Date == date)
                    .ToList();

                return new SellerDashboardRevenueTrendPointDto
                {
                    Label = date.ToString("dd MMM", AnalyticsCulture),
                    Date = DateOnly.FromDateTime(date),
                    Orders = matchingOrders.Count(order => order.OrderItems.Any(item => item.Product.SellerId == sellerId)),
                    Revenue = Math.Round(CalculateSellerRevenue(matchingOrders, sellerId), 2)
                };
            })
            .ToList();
    }

    private static List<SellerDashboardRevenueTrendPointDto> BuildWeeklyDashboardTrend(IEnumerable<Order> orders, int sellerId)
    {
        var today = DateTime.UtcNow.Date;
        var currentWeekStart = StartOfWeek(today);

        return Enumerable.Range(0, 8)
            .Select(offset => currentWeekStart.AddDays(-(7 * (7 - offset))))
            .Select(start =>
            {
                var end = start.AddDays(7);
                var matchingOrders = orders
                    .Where(order => order.CreatedAt.Date >= start && order.CreatedAt.Date < end)
                    .ToList();

                return new SellerDashboardRevenueTrendPointDto
                {
                    Label = start.ToString("dd MMM", AnalyticsCulture),
                    Date = DateOnly.FromDateTime(start),
                    Orders = matchingOrders.Count(order => order.OrderItems.Any(item => item.Product.SellerId == sellerId)),
                    Revenue = Math.Round(CalculateSellerRevenue(matchingOrders, sellerId), 2)
                };
            })
            .ToList();
    }

    private static List<SellerDashboardRevenueTrendPointDto> BuildMonthlyDashboardTrend(IEnumerable<Order> orders, int sellerId)
    {
        var today = DateTime.UtcNow.Date;
        var currentMonthStart = new DateTime(today.Year, today.Month, 1);

        return Enumerable.Range(0, 6)
            .Select(offset => currentMonthStart.AddMonths(-(5 - offset)))
            .Select(start =>
            {
                var end = start.AddMonths(1);
                var matchingOrders = orders
                    .Where(order => order.CreatedAt.Date >= start && order.CreatedAt.Date < end)
                    .ToList();

                return new SellerDashboardRevenueTrendPointDto
                {
                    Label = start.ToString("MMM yyyy", AnalyticsCulture),
                    Date = DateOnly.FromDateTime(start),
                    Orders = matchingOrders.Count(order => order.OrderItems.Any(item => item.Product.SellerId == sellerId)),
                    Revenue = Math.Round(CalculateSellerRevenue(matchingOrders, sellerId), 2)
                };
            })
            .ToList();
    }

    private static decimal CalculateSellerRevenue(IEnumerable<Order> orders, int sellerId)
    {
        return orders
            .Where(order => order.Payment?.Status == PaymentStatus.Success && order.Status != OrderStatus.Cancelled)
            .SelectMany(order => order.OrderItems.Where(item => item.Product.SellerId == sellerId))
            .Sum(item => item.PriceSnapshot * item.Quantity);
    }

    private static decimal CalculateDelta(decimal current, decimal previous)
    {
        if (previous == 0)
        {
            return current == 0 ? 0 : 100;
        }

        return Math.Round((current - previous) / previous * 100m, 2);
    }

    private async Task<decimal> GetCommissionRatePercentAsync(int sellerId)
    {
        var sellerProfile = await _sellerProfileDal.GetAsync(profile => profile.Id == sellerId);
        if (sellerProfile?.CommissionRateOverride is >= 0 and <= 100)
        {
            return sellerProfile.CommissionRateOverride.Value;
        }

        return DefaultCommissionRate * 100m;
    }

    private static DateTime StartOfWeek(DateTime date)
    {
        var diff = (7 + (date.DayOfWeek - DayOfWeek.Monday)) % 7;
        return date.AddDays(-1 * diff).Date;
    }

    private static CultureInfo ResolveAnalyticsCulture()
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

    private static decimal CalculateRatio(long numerator, long denominator)
    {
        if (denominator <= 0 || numerator <= 0)
        {
            return 0;
        }

        return Math.Round((decimal)numerator / denominator * 100m, 2);
    }

    private static decimal CalculateRatio(int numerator, int denominator)
    {
        if (denominator <= 0 || numerator <= 0)
        {
            return 0;
        }

        return Math.Round((decimal)numerator / denominator * 100m, 2);
    }
}
