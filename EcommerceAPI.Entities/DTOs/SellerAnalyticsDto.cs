using EcommerceAPI.Core.Entities;

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

public class SellerAnalyticsTrendPointDto : IDto
{
    public DateOnly Date { get; set; }
    public long Views { get; set; }
    public int Favorites { get; set; }
    public int Orders { get; set; }
    public decimal Revenue { get; set; }
    public decimal AverageRating { get; set; }
}
