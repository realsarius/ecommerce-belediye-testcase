using EcommerceAPI.Application.Abstractions.ServiceContracts;
using EcommerceAPI.Core.Utilities.Results;
using EcommerceAPI.Application.Abstractions.Persistence;
using EcommerceAPI.Entities.Concrete;
using EcommerceAPI.Entities.DTOs;
using EcommerceAPI.Entities.Enums;

namespace EcommerceAPI.Business.Concrete;

public class AdminFinanceManager : IAdminFinanceService
{
    private const decimal DefaultCommissionRate = 10m;

    private static readonly OrderStatus[] RevenueStatuses =
    {
        OrderStatus.Paid,
        OrderStatus.Processing,
        OrderStatus.Shipped,
        OrderStatus.Delivered
    };

    private readonly IOrderDal _orderDal;
    private readonly ISellerProfileDal _sellerProfileDal;

    public AdminFinanceManager(
        IOrderDal orderDal,
        ISellerProfileDal sellerProfileDal)
    {
        _orderDal = orderDal;
        _sellerProfileDal = sellerProfileDal;
    }

    public async Task<IDataResult<AdminFinanceSummaryDto>> GetSummaryAsync(DateTime? from = null, DateTime? to = null)
    {
        if (from.HasValue && to.HasValue && from.Value.Date > to.Value.Date)
        {
            return new ErrorDataResult<AdminFinanceSummaryDto>("Başlangıç tarihi bitiş tarihinden büyük olamaz.");
        }

        var orders = (await _orderDal.GetAllOrdersWithDetailsAsync()).ToList();
        var sellerProfiles = await _sellerProfileDal.GetAdminListWithDetailsAsync();

        var sellerProductMap = sellerProfiles
            .SelectMany(profile => profile.Products.Select(product => new { product.Id, Profile = profile }))
            .ToDictionary(item => item.Id, item => item.Profile);

        var startDate = from?.Date;
        var endDateExclusive = to?.Date.AddDays(1);

        var filteredOrders = orders
            .Where(order => !startDate.HasValue || order.CreatedAt >= startDate.Value)
            .Where(order => !endDateExclusive.HasValue || order.CreatedAt < endDateExclusive.Value)
            .ToList();

        var rows = BuildSellerRows(filteredOrders, sellerProductMap);
        var totalRevenue = rows.Sum(row => row.GrossSales);
        var totalCommission = rows.Sum(row => row.CommissionAmount);
        var totalRefundAmount = rows.Sum(row => row.RefundedAmount);
        var successfulOrderCount = filteredOrders.Count(order => RevenueStatuses.Contains(order.Status));

        var summary = new AdminFinanceSummaryDto
        {
            FromDate = startDate,
            ToDate = to?.Date,
            TotalRevenue = Math.Round(totalRevenue, 2),
            TotalCommission = Math.Round(totalCommission, 2),
            AverageOrderValue = successfulOrderCount > 0 ? Math.Round(totalRevenue / successfulOrderCount, 2) : 0,
            TotalRefundAmount = Math.Round(totalRefundAmount, 2),
            SuccessfulOrderCount = successfulOrderCount,
            Currency = filteredOrders.FirstOrDefault()?.Currency ?? "TRY",
            Sellers = rows
        };

        return new SuccessDataResult<AdminFinanceSummaryDto>(summary);
    }

    private static List<AdminFinanceSellerRowDto> BuildSellerRows(
        IEnumerable<Order> orders,
        IReadOnlyDictionary<int, SellerProfile> sellerProductMap)
    {
        var rows = new Dictionary<string, AdminFinanceSellerRowDto>();

        foreach (var order in orders)
        {
            var sellerKeysForSuccessfulOrder = new HashSet<string>();

            foreach (var item in order.OrderItems)
            {
                var sellerProfile = item.Product != null && sellerProductMap.TryGetValue(item.ProductId, out var profile)
                    ? profile
                    : null;

                var sellerId = sellerProfile?.Id;
                var sellerName = sellerProfile?.BrandName ?? "Atanmamış";
                var sellerKey = sellerId?.ToString() ?? sellerName;

                if (!rows.TryGetValue(sellerKey, out var row))
                {
                    var commissionRate = sellerProfile?.CommissionRateOverride ?? DefaultCommissionRate;
                    row = new AdminFinanceSellerRowDto
                    {
                        SellerId = sellerId,
                        SellerName = sellerName,
                        CommissionRate = commissionRate
                    };
                    rows[sellerKey] = row;
                }

                var lineTotal = item.PriceSnapshot * item.Quantity;

                if (RevenueStatuses.Contains(order.Status))
                {
                    row.GrossSales += lineTotal;
                    sellerKeysForSuccessfulOrder.Add(sellerKey);
                }

                if (order.Status == OrderStatus.Refunded)
                {
                    row.RefundedAmount += lineTotal;
                }
            }

            foreach (var sellerKey in sellerKeysForSuccessfulOrder)
            {
                rows[sellerKey].SuccessfulOrders += 1;
            }
        }

        return rows.Values
            .Select(row =>
            {
                row.NetSales = Math.Round(Math.Max(0, row.GrossSales - row.RefundedAmount), 2);
                row.CommissionAmount = Math.Round(row.NetSales * (row.CommissionRate / 100), 2);
                row.NetEarnings = Math.Round(Math.Max(0, row.NetSales - row.CommissionAmount), 2);
                row.GrossSales = Math.Round(row.GrossSales, 2);
                row.RefundedAmount = Math.Round(row.RefundedAmount, 2);
                return row;
            })
            .OrderByDescending(row => row.NetSales)
            .ThenBy(row => row.SellerName)
            .ToList();
    }
}
