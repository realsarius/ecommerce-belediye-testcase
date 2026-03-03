using EcommerceAPI.Core.Entities;

namespace EcommerceAPI.Entities.DTOs;

public class AdminFinanceSellerRowDto : IDto
{
    public int? SellerId { get; set; }
    public string SellerName { get; set; } = string.Empty;
    public decimal GrossSales { get; set; }
    public decimal RefundedAmount { get; set; }
    public decimal NetSales { get; set; }
    public int SuccessfulOrders { get; set; }
    public decimal CommissionRate { get; set; }
    public decimal CommissionAmount { get; set; }
    public decimal NetEarnings { get; set; }
}

public class AdminFinanceSummaryDto : IDto
{
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal TotalCommission { get; set; }
    public decimal AverageOrderValue { get; set; }
    public decimal TotalRefundAmount { get; set; }
    public int SuccessfulOrderCount { get; set; }
    public string Currency { get; set; } = "TRY";
    public List<AdminFinanceSellerRowDto> Sellers { get; set; } = new();
}
