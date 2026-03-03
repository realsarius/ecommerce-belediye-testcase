using EcommerceAPI.Core.Entities;

namespace EcommerceAPI.Entities.DTOs;

public class AdminSellerListItemDto : IDto
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string BrandName { get; set; } = string.Empty;
    public string SellerFirstName { get; set; } = string.Empty;
    public string SellerLastName { get; set; } = string.Empty;
    public string OwnerEmail { get; set; } = string.Empty;
    public string Status { get; set; } = "Pending";
    public int ProductCount { get; set; }
    public int ActiveProductCount { get; set; }
    public int TotalStock { get; set; }
    public decimal TotalSales { get; set; }
    public double AverageRating { get; set; }
    public decimal CommissionRate { get; set; }
    public bool HasCommissionOverride { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsVerified { get; set; }
}

public class AdminSellerProductSummaryDto : IDto
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Currency { get; set; } = "TRY";
    public int StockQuantity { get; set; }
    public bool IsActive { get; set; }
    public double AverageRating { get; set; }
}

public class AdminSellerDetailDto : IDto
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string BrandName { get; set; } = string.Empty;
    public string? BrandDescription { get; set; }
    public string? LogoUrl { get; set; }
    public string? BannerImageUrl { get; set; }
    public string? ContactEmail { get; set; }
    public string? ContactPhone { get; set; }
    public string? WebsiteUrl { get; set; }
    public string? InstagramUrl { get; set; }
    public string? FacebookUrl { get; set; }
    public string? XUrl { get; set; }
    public bool IsVerified { get; set; }
    public string Status { get; set; } = "Pending";
    public string SellerFirstName { get; set; } = string.Empty;
    public string SellerLastName { get; set; } = string.Empty;
    public string OwnerEmail { get; set; } = string.Empty;
    public int ProductCount { get; set; }
    public int ActiveProductCount { get; set; }
    public int TotalStock { get; set; }
    public decimal TotalSales { get; set; }
    public double AverageRating { get; set; }
    public decimal CommissionRate { get; set; }
    public decimal? CommissionRateOverride { get; set; }
    public string Currency { get; set; } = "TRY";
    public string? ApplicationReviewNote { get; set; }
    public DateTime? ApplicationReviewedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<AdminSellerProductSummaryDto> Products { get; set; } = new();
}

public class UpdateAdminSellerStatusRequest : IDto
{
    public string Status { get; set; } = string.Empty;
    public string? ReviewNote { get; set; }
}

public class UpdateAdminSellerCommissionRequest : IDto
{
    public decimal? Rate { get; set; }
}

public class ReviewSellerApplicationRequest : IDto
{
    public string? ReviewNote { get; set; }
}
