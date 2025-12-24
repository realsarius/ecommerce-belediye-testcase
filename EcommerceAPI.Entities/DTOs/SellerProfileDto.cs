using EcommerceAPI.Core.Entities;

namespace EcommerceAPI.Entities.DTOs;

public class SellerProfileDto : IDto
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string BrandName { get; set; } = string.Empty;
    public string? BrandDescription { get; set; }
    public string? LogoUrl { get; set; }
    public bool IsVerified { get; set; }
    public DateTime CreatedAt { get; set; }
    
    // User info for display
    public string SellerFirstName { get; set; } = string.Empty;
    public string SellerLastName { get; set; } = string.Empty;
}

public class CreateSellerProfileRequest : IDto
{
    public string BrandName { get; set; } = string.Empty;
    public string? BrandDescription { get; set; }
    public string? LogoUrl { get; set; }
}

public class UpdateSellerProfileRequest : IDto
{
    public string? BrandName { get; set; }
    public string? BrandDescription { get; set; }
    public string? LogoUrl { get; set; }
}
