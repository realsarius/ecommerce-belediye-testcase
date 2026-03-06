using System.Text;
using EcommerceAPI.Application.Abstractions.ServiceContracts;
using EcommerceAPI.Entities.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EcommerceAPI.API.Controllers;

[Route("api/v1/admin/finance")]
[Authorize(Roles = "Admin")]
public class AdminFinanceController : BaseApiController
{
    private readonly IAdminFinanceService _adminFinanceService;

    public AdminFinanceController(IAdminFinanceService adminFinanceService)
    {
        _adminFinanceService = adminFinanceService;
    }

    [HttpGet]
    public async Task<IActionResult> GetSummary([FromQuery] DateTime? from = null, [FromQuery] DateTime? to = null)
    {
        var result = await _adminFinanceService.GetSummaryAsync(from, to);
        return HandleResult(result);
    }

    [HttpGet("export")]
    public async Task<IActionResult> Export([FromQuery] DateTime? from = null, [FromQuery] DateTime? to = null)
    {
        var result = await _adminFinanceService.GetSummaryAsync(from, to);
        if (!result.Success)
        {
            return HandleResult(result);
        }

        var csv = BuildCsv(result.Data);
        var fileName = $"admin-finance-{(from ?? DateTime.UtcNow):yyyy-MM-dd}-{(to ?? DateTime.UtcNow):yyyy-MM-dd}.csv";
        return File(Encoding.UTF8.GetBytes(csv), "text/csv", fileName);
    }

    private static string BuildCsv(AdminFinanceSummaryDto summary)
    {
        var lines = new List<string>
        {
            "\"Rapor\",\"Deger\"",
            $"\"Baslangic Tarihi\",\"{summary.FromDate:yyyy-MM-dd}\"",
            $"\"Bitis Tarihi\",\"{summary.ToDate:yyyy-MM-dd}\"",
            $"\"Toplam Gelir\",\"{summary.TotalRevenue}\"",
            $"\"Toplam Komisyon\",\"{summary.TotalCommission}\"",
            $"\"Ortalama Siparis Degeri\",\"{summary.AverageOrderValue}\"",
            $"\"Toplam Iade Tutari\",\"{summary.TotalRefundAmount}\"",
            string.Empty,
            "\"Seller\",\"Basarili Siparis\",\"Brut Satis\",\"Iade\",\"Net Satis\",\"Komisyon Orani\",\"Komisyon Tutari\",\"Net Kazanc\""
        };

        lines.AddRange(summary.Sellers.Select(row =>
            $"\"{Escape(row.SellerName)}\",\"{row.SuccessfulOrders}\",\"{row.GrossSales}\",\"{row.RefundedAmount}\",\"{row.NetSales}\",\"{row.CommissionRate}\",\"{row.CommissionAmount}\",\"{row.NetEarnings}\""));

        return string.Join(Environment.NewLine, lines);
    }

    private static string Escape(string value) => value.Replace("\"", "\"\"");
}
