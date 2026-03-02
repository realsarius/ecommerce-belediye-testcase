using EcommerceAPI.Core.Entities;
using EcommerceAPI.Entities.Enums;

namespace EcommerceAPI.Entities.DTOs;

public class GiftCardDto : IDto
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string MaskedCode { get; set; } = string.Empty;
    public decimal InitialBalance { get; set; }
    public decimal CurrentBalance { get; set; }
    public string Currency { get; set; } = "TRY";
    public bool IsActive { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public bool IsAssigned { get; set; }
    public DateTime? AssignedAt { get; set; }
    public DateTime? LastUsedAt { get; set; }
    public string? Description { get; set; }
    public string? AssignedUserEmail { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class GiftCardTransactionDto : IDto
{
    public int Id { get; set; }
    public int GiftCardId { get; set; }
    public string GiftCardCode { get; set; } = string.Empty;
    public string MaskedGiftCardCode { get; set; } = string.Empty;
    public int? OrderId { get; set; }
    public string? OrderNumber { get; set; }
    public GiftCardTransactionType Type { get; set; }
    public decimal Amount { get; set; }
    public decimal BalanceAfter { get; set; }
    public string Description { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class GiftCardSummaryDto : IDto
{
    public decimal TotalAvailableBalance { get; set; }
    public int ActiveCardCount { get; set; }
    public List<GiftCardDto> Cards { get; set; } = new();
    public List<GiftCardTransactionDto> RecentTransactions { get; set; } = new();
}

public class CreateGiftCardRequest : IDto
{
    public string? Code { get; set; }
    public decimal InitialBalance { get; set; }
    public int? ValidDays { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public string? Description { get; set; }
}

public class UpdateGiftCardRequest : IDto
{
    public bool? IsActive { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public string? Description { get; set; }
}

public class ValidateGiftCardRequest : IDto
{
    public string Code { get; set; } = string.Empty;
    public decimal OrderTotal { get; set; }
}

public class GiftCardValidationResult : IDto
{
    public bool IsValid { get; set; }
    public string? ErrorMessage { get; set; }
    public int GiftCardId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string MaskedCode { get; set; } = string.Empty;
    public decimal AvailableBalance { get; set; }
    public decimal AppliedAmount { get; set; }
    public decimal RemainingBalance { get; set; }
    public decimal FinalTotal { get; set; }
}
