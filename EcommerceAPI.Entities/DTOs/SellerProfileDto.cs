using EcommerceAPI.Core.Entities;

namespace EcommerceAPI.Entities.DTOs;

public class SellerProfileDto : IDto
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
    public decimal? CommissionRateOverride { get; set; }
    public string? ApplicationReviewNote { get; set; }
    public DateTime? ApplicationReviewedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    

    public string SellerFirstName { get; set; } = string.Empty;
    public string SellerLastName { get; set; } = string.Empty;
}

public class CreateSellerProfileRequest : IDto
{
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
}

public class UpdateSellerProfileRequest : IDto
{
    public string? BrandName { get; set; }
    public string? BrandDescription { get; set; }
    public string? LogoUrl { get; set; }
    public string? BannerImageUrl { get; set; }
    public string? ContactEmail { get; set; }
    public string? ContactPhone { get; set; }
    public string? WebsiteUrl { get; set; }
    public string? InstagramUrl { get; set; }
    public string? FacebookUrl { get; set; }
    public string? XUrl { get; set; }
}
