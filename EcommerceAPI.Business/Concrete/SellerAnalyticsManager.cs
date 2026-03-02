using System.Globalization;
using EcommerceAPI.Business.Abstract;
using EcommerceAPI.Core.Interfaces;
using EcommerceAPI.Core.Utilities.Results;
using EcommerceAPI.DataAccess.Abstract;
using EcommerceAPI.Entities.Concrete;
using EcommerceAPI.Entities.DTOs;
using EcommerceAPI.Entities.Enums;

namespace EcommerceAPI.Business.Concrete;

public class SellerAnalyticsManager : ISellerAnalyticsService
{
    private const decimal DefaultCommissionRate = 0.10m;

    private readonly IProductDal _productDal;
    private readonly IOrderDal _orderDal;
    private readonly IReturnRequestDal _returnRequestDal;
    private readonly IProductReviewDal _productReviewDal;
    private readonly IWishlistItemDal _wishlistItemDal;
    private readonly IRecommendationCacheService _recommendationCacheService;

    public SellerAnalyticsManager(
        IProductDal productDal,
        IOrderDal orderDal,
        IReturnRequestDal returnRequestDal,
        IProductReviewDal productReviewDal,
        IWishlistItemDal wishlistItemDal,
        IRecommendationCacheService recommendationCacheService)
    {
        _productDal = productDal;
        _orderDal = orderDal;
        _returnRequestDal = returnRequestDal;
        _productReviewDal = productReviewDal;
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

    public async Task<IDataResult<SellerFinanceSummaryDto>> GetFinanceSummaryAsync(int sellerId, int days = 30)
    {
        var normalizedDays = Math.Clamp(days, 7, 90);
        var fromDate = DateTime.UtcNow.Date.AddDays(-(normalizedDays - 1));

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
                CommissionRate = DefaultCommissionRate * 100m,
                Currency = currency
            });
        }

        var lifetimeGrossRevenue = orders
            .Where(order => order.Status != OrderStatus.Cancelled)
            .SelectMany(order => order.OrderItems.Where(item => item.Product.SellerId == sellerId))
            .Sum(item => item.PriceSnapshot * item.Quantity);

        var periodOrders = orders
            .Where(order => order.CreatedAt >= fromDate)
            .ToList();

        var dailyTrend = Enumerable.Range(0, normalizedDays)
            .Select(offset =>
            {
                var date = DateOnly.FromDateTime(fromDate.AddDays(offset));
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
                var commissionAmount = Math.Round(netSales * DefaultCommissionRate, 2);

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
                    MonthLabel = representativeDate.ToString("MMMM yyyy", CultureInfo.GetCultureInfo("tr-TR")),
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
            TotalOrders = totalOrders,
            GrossSales = Math.Round(grossSalesTotal, 2),
            RefundedAmount = refundedAmountTotal,
            NetSales = Math.Round(netSalesTotal, 2),
            CommissionRate = DefaultCommissionRate * 100m,
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

    private static List<SellerAnalyticsTrendPointDto> CreateEmptyTrend(DateOnly fromDate, int days)
    {
        return Enumerable.Range(0, days)
            .Select(offset => new SellerAnalyticsTrendPointDto
            {
                Date = fromDate.AddDays(offset)
            })
            .ToList();
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
