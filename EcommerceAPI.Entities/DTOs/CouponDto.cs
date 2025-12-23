using EcommerceAPI.Entities.Enums;

namespace EcommerceAPI.Entities.DTOs;

// Response DTO
public class CouponDto
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public CouponType Type { get; set; }
    public decimal Value { get; set; }
    public decimal? MinOrderAmount { get; set; }
    public int UsageLimit { get; set; }
    public int UsedCount { get; set; }
    public DateTime ExpiresAt { get; set; }
    public bool IsActive { get; set; }
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
}

// Create Request
public class CreateCouponRequest
{
    public string Code { get; set; } = string.Empty;
    public CouponType Type { get; set; }
    public decimal Value { get; set; }
    public decimal? MinOrderAmount { get; set; }
    public int UsageLimit { get; set; }
    public int ValidDays { get; set; } = 7;  // Varsayılan 7 gün geçerli
    public string? Description { get; set; }
}

// Update Request
public class UpdateCouponRequest
{
    public string? Code { get; set; }
    public CouponType? Type { get; set; }
    public decimal? Value { get; set; }
    public decimal? MinOrderAmount { get; set; }
    public int? UsageLimit { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public bool? IsActive { get; set; }
    public string? Description { get; set; }
}

// Validate Request
public class ValidateCouponRequest
{
    public string Code { get; set; } = string.Empty;
    public decimal OrderTotal { get; set; }
}

// Validate Response
public class CouponValidationResult
{
    public bool IsValid { get; set; }
    public string? ErrorMessage { get; set; }
    public CouponDto? Coupon { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal FinalTotal { get; set; }
}
