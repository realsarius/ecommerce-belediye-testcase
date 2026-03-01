using EcommerceAPI.Entities.Enums;

namespace EcommerceAPI.Entities.DTOs;

public class CampaignDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? BadgeText { get; set; }
    public CampaignType Type { get; set; }
    public CampaignStatus Status { get; set; }
    public bool IsEnabled { get; set; }
    public DateTime StartsAt { get; set; }
    public DateTime EndsAt { get; set; }
    public List<CampaignProductDto> Products { get; set; } = new();
}

public class CampaignProductDto
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string ProductSku { get; set; } = string.Empty;
    public decimal OriginalPrice { get; set; }
    public decimal CampaignPrice { get; set; }
    public bool IsFeatured { get; set; }
}

public class CreateCampaignRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? BadgeText { get; set; }
    public CampaignType Type { get; set; } = CampaignType.FlashSale;
    public bool IsEnabled { get; set; } = true;
    public DateTime StartsAt { get; set; }
    public DateTime EndsAt { get; set; }
    public List<CreateCampaignProductRequest> Products { get; set; } = new();
}

public class CreateCampaignProductRequest
{
    public int ProductId { get; set; }
    public decimal CampaignPrice { get; set; }
    public bool IsFeatured { get; set; }
}

public class UpdateCampaignRequest
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? BadgeText { get; set; }
    public CampaignType? Type { get; set; }
    public bool? IsEnabled { get; set; }
    public DateTime? StartsAt { get; set; }
    public DateTime? EndsAt { get; set; }
    public List<CreateCampaignProductRequest>? Products { get; set; }
}
