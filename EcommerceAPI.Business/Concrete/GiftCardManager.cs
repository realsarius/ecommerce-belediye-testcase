using EcommerceAPI.Business.Abstract;
using EcommerceAPI.Core.Aspects.Autofac.Logging;
using EcommerceAPI.Core.CrossCuttingConcerns.Logging;
using EcommerceAPI.Core.Interfaces;
using EcommerceAPI.Core.Utilities.Results;
using EcommerceAPI.DataAccess.Abstract;
using EcommerceAPI.Entities.Concrete;
using EcommerceAPI.Entities.DTOs;
using EcommerceAPI.Entities.Enums;

namespace EcommerceAPI.Business.Concrete;

public class GiftCardManager : IGiftCardService
{
    private readonly IGiftCardDal _giftCardDal;
    private readonly IGiftCardTransactionDal _giftCardTransactionDal;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditService _auditService;

    public GiftCardManager(
        IGiftCardDal giftCardDal,
        IGiftCardTransactionDal giftCardTransactionDal,
        IUnitOfWork unitOfWork,
        IAuditService auditService)
    {
        _giftCardDal = giftCardDal;
        _giftCardTransactionDal = giftCardTransactionDal;
        _unitOfWork = unitOfWork;
        _auditService = auditService;
    }

    [LogAspect]
    public async Task<IDataResult<List<GiftCardDto>>> GetAllAsync()
    {
        var giftCards = await _giftCardDal.GetAllWithAssignedUserAsync();
        return new SuccessDataResult<List<GiftCardDto>>(giftCards.Select(MapToDto).ToList());
    }

    [LogAspect]
    public async Task<IDataResult<GiftCardDto>> GetByIdAsync(int id)
    {
        var giftCard = await _giftCardDal.GetByIdWithAssignedUserAsync(id);
        if (giftCard == null)
        {
            return new ErrorDataResult<GiftCardDto>("Gift card bulunamadı.");
        }

        return new SuccessDataResult<GiftCardDto>(MapToDto(giftCard));
    }

    [LogAspect]
    public async Task<IDataResult<GiftCardDto>> CreateAsync(CreateGiftCardRequest request)
    {
        if (request.InitialBalance <= 0)
        {
            return new ErrorDataResult<GiftCardDto>("Gift card bakiyesi 0'dan büyük olmalıdır.");
        }

        var normalizedCode = NormalizeCode(request.Code) ?? GenerateCode();
        var existing = await _giftCardDal.GetByCodeAsync(normalizedCode);
        if (existing != null)
        {
            return new ErrorDataResult<GiftCardDto>("Bu gift card kodu zaten kullanılıyor.");
        }

        var expiresAt = request.ExpiresAt;
        if (!expiresAt.HasValue && request.ValidDays.HasValue && request.ValidDays.Value > 0)
        {
            expiresAt = DateTime.UtcNow.AddDays(request.ValidDays.Value);
        }

        var giftCard = new GiftCard
        {
            Code = normalizedCode,
            InitialBalance = request.InitialBalance,
            CurrentBalance = request.InitialBalance,
            ExpiresAt = expiresAt,
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim()
        };

        await _giftCardDal.AddAsync(giftCard);
        await _unitOfWork.SaveChangesAsync();

        await _giftCardTransactionDal.AddAsync(new GiftCardTransaction
        {
            GiftCardId = giftCard.Id,
            Type = GiftCardTransactionType.Issued,
            Amount = giftCard.InitialBalance,
            BalanceAfter = giftCard.CurrentBalance,
            Description = "Gift card oluşturuldu"
        });

        await _unitOfWork.SaveChangesAsync();

        await _auditService.LogActionAsync(
            "Admin",
            "CreateGiftCard",
            "GiftCard",
            new { giftCard.Id, giftCard.Code, giftCard.InitialBalance, giftCard.ExpiresAt });

        return new SuccessDataResult<GiftCardDto>(MapToDto(giftCard), "Gift card oluşturuldu.");
    }

    [LogAspect]
    public async Task<IDataResult<GiftCardDto>> UpdateAsync(int id, UpdateGiftCardRequest request)
    {
        var giftCard = await _giftCardDal.GetByIdWithAssignedUserAsync(id);
        if (giftCard == null)
        {
            return new ErrorDataResult<GiftCardDto>("Gift card bulunamadı.");
        }

        if (request.IsActive.HasValue)
        {
            giftCard.IsActive = request.IsActive.Value;
        }

        if (request.ExpiresAt.HasValue)
        {
            giftCard.ExpiresAt = request.ExpiresAt.Value;
        }

        if (request.Description != null)
        {
            giftCard.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
        }

        giftCard.UpdatedAt = DateTime.UtcNow;
        _giftCardDal.Update(giftCard);
        await _unitOfWork.SaveChangesAsync();

        await _auditService.LogActionAsync(
            "Admin",
            "UpdateGiftCard",
            "GiftCard",
            new { giftCard.Id, giftCard.Code, giftCard.IsActive, giftCard.ExpiresAt });

        return new SuccessDataResult<GiftCardDto>(MapToDto(giftCard), "Gift card güncellendi.");
    }

    [LogAspect]
    public async Task<IDataResult<List<GiftCardDto>>> GetUserGiftCardsAsync(int userId)
    {
        var giftCards = await _giftCardDal.GetUserGiftCardsAsync(userId);
        return new SuccessDataResult<List<GiftCardDto>>(giftCards.Select(MapToDto).ToList());
    }

    [LogAspect]
    public async Task<IDataResult<GiftCardSummaryDto>> GetSummaryAsync(int userId, int recentLimit = 10)
    {
        var giftCards = await _giftCardDal.GetUserGiftCardsAsync(userId);
        var now = DateTime.UtcNow;
        var activeCards = giftCards
            .Where(x => x.IsActive && x.CurrentBalance > 0 && (!x.ExpiresAt.HasValue || x.ExpiresAt > now))
            .ToList();

        var transactions = await _giftCardTransactionDal.GetUserTransactionsAsync(userId, recentLimit);

        return new SuccessDataResult<GiftCardSummaryDto>(new GiftCardSummaryDto
        {
            TotalAvailableBalance = activeCards.Sum(x => x.CurrentBalance),
            ActiveCardCount = activeCards.Count,
            Cards = giftCards.Select(MapToDto).ToList(),
            RecentTransactions = transactions.Select(MapTransactionToDto).ToList()
        });
    }

    [LogAspect]
    public async Task<IDataResult<List<GiftCardTransactionDto>>> GetHistoryAsync(int userId, int limit = 50)
    {
        var transactions = await _giftCardTransactionDal.GetUserTransactionsAsync(userId, limit);
        return new SuccessDataResult<List<GiftCardTransactionDto>>(transactions.Select(MapTransactionToDto).ToList());
    }

    [LogAspect]
    public async Task<IDataResult<GiftCardValidationResult>> ValidateAsync(int userId, string code, decimal orderTotal)
    {
        var normalizedCode = NormalizeCode(code);
        if (string.IsNullOrEmpty(normalizedCode))
        {
            return new ErrorDataResult<GiftCardValidationResult>("Gift card kodu gereklidir.");
        }

        if (orderTotal <= 0)
        {
            return new ErrorDataResult<GiftCardValidationResult>("Gift card uygulanacak tutar bulunamadı.");
        }

        var giftCard = await _giftCardDal.GetByCodeAsync(normalizedCode);
        if (giftCard == null)
        {
            return new ErrorDataResult<GiftCardValidationResult>("Gift card bulunamadı.");
        }

        var eligibilityError = GetEligibilityError(giftCard, userId);
        if (eligibilityError != null)
        {
            return new ErrorDataResult<GiftCardValidationResult>(eligibilityError);
        }

        var appliedAmount = Math.Min(giftCard.CurrentBalance, orderTotal);

        return new SuccessDataResult<GiftCardValidationResult>(new GiftCardValidationResult
        {
            IsValid = true,
            GiftCardId = giftCard.Id,
            Code = giftCard.Code,
            MaskedCode = MaskCode(giftCard.Code),
            AvailableBalance = giftCard.CurrentBalance,
            AppliedAmount = appliedAmount,
            RemainingBalance = giftCard.CurrentBalance - appliedAmount,
            FinalTotal = orderTotal - appliedAmount
        });
    }

    [LogAspect]
    public async Task<IResult> RedeemForOrderAsync(int userId, int orderId, string code, decimal amount, string description)
    {
        if (amount <= 0)
        {
            return new SuccessResult();
        }

        var existing = await _giftCardTransactionDal.GetByOrderAndTypeAsync(orderId, GiftCardTransactionType.Redeemed);
        if (existing != null)
        {
            return new SuccessResult("Gift card kullanımı zaten işlendi.");
        }

        var validation = await ValidateAsync(userId, code, amount);
        if (!validation.Success || !validation.Data.IsValid)
        {
            return new ErrorResult(validation.Message);
        }

        var giftCard = await _giftCardDal.GetByCodeAsync(validation.Data.Code);
        if (giftCard == null)
        {
            return new ErrorResult("Gift card bulunamadı.");
        }

        if (giftCard.AssignedUserId == null)
        {
            giftCard.AssignedUserId = userId;
            giftCard.AssignedAt = DateTime.UtcNow;
        }

        if (giftCard.CurrentBalance < amount)
        {
            return new ErrorResult("Gift card bakiyesi yetersiz.");
        }

        giftCard.CurrentBalance -= amount;
        giftCard.LastUsedAt = DateTime.UtcNow;
        giftCard.UpdatedAt = DateTime.UtcNow;

        await _giftCardTransactionDal.AddAsync(new GiftCardTransaction
        {
            GiftCardId = giftCard.Id,
            UserId = userId,
            OrderId = orderId,
            Type = GiftCardTransactionType.Redeemed,
            Amount = -amount,
            BalanceAfter = giftCard.CurrentBalance,
            Description = description
        });

        _giftCardDal.Update(giftCard);

        await _auditService.LogActionAsync(
            userId.ToString(),
            "RedeemGiftCard",
            "GiftCardTransaction",
            new { giftCard.Id, giftCard.Code, OrderId = orderId, Amount = amount });

        return new SuccessResult();
    }

    [LogAspect]
    public async Task<IResult> RestoreForOrderAsync(int userId, int orderId, string description)
    {
        var redeemed = await _giftCardTransactionDal.GetByOrderAndTypeAsync(orderId, GiftCardTransactionType.Redeemed);
        if (redeemed == null)
        {
            return new SuccessResult();
        }

        var restored = await _giftCardTransactionDal.GetByOrderAndTypeAsync(orderId, GiftCardTransactionType.Restored);
        if (restored != null)
        {
            return new SuccessResult("Gift card iadesi zaten işlendi.");
        }

        var giftCard = redeemed.GiftCard;
        if (giftCard == null)
        {
            return new ErrorResult("Gift card bulunamadı.");
        }

        var restoreAmount = Math.Abs(redeemed.Amount);
        giftCard.CurrentBalance += restoreAmount;
        giftCard.UpdatedAt = DateTime.UtcNow;

        await _giftCardTransactionDal.AddAsync(new GiftCardTransaction
        {
            GiftCardId = giftCard.Id,
            UserId = userId,
            OrderId = orderId,
            Type = GiftCardTransactionType.Restored,
            Amount = restoreAmount,
            BalanceAfter = giftCard.CurrentBalance,
            Description = description
        });

        _giftCardDal.Update(giftCard);

        await _auditService.LogActionAsync(
            userId.ToString(),
            "RestoreGiftCard",
            "GiftCardTransaction",
            new { giftCard.Id, giftCard.Code, OrderId = orderId, Amount = restoreAmount });

        return new SuccessResult();
    }

    private static string? NormalizeCode(string? code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return null;
        }

        return code.Trim().ToUpperInvariant();
    }

    private static string GenerateCode()
    {
        return $"GC-{Guid.NewGuid():N}"[..15].ToUpperInvariant();
    }

    private static string? GetEligibilityError(GiftCard giftCard, int userId)
    {
        if (!giftCard.IsActive)
        {
            return "Bu gift card aktif değil.";
        }

        if (giftCard.ExpiresAt.HasValue && giftCard.ExpiresAt.Value <= DateTime.UtcNow)
        {
            return "Gift card süresi dolmuş.";
        }

        if (giftCard.CurrentBalance <= 0)
        {
            return "Gift card bakiyesi tükenmiş.";
        }

        if (giftCard.AssignedUserId.HasValue && giftCard.AssignedUserId.Value != userId)
        {
            return "Bu gift card başka bir kullanıcıya ait.";
        }

        return null;
    }

    private static GiftCardDto MapToDto(GiftCard giftCard)
    {
        return new GiftCardDto
        {
            Id = giftCard.Id,
            Code = giftCard.Code,
            MaskedCode = MaskCode(giftCard.Code),
            InitialBalance = giftCard.InitialBalance,
            CurrentBalance = giftCard.CurrentBalance,
            Currency = giftCard.Currency,
            IsActive = giftCard.IsActive,
            ExpiresAt = giftCard.ExpiresAt,
            IsAssigned = giftCard.AssignedUserId.HasValue,
            AssignedAt = giftCard.AssignedAt,
            LastUsedAt = giftCard.LastUsedAt,
            Description = giftCard.Description,
            AssignedUserEmail = giftCard.AssignedUser?.Email,
            CreatedAt = giftCard.CreatedAt
        };
    }

    private static GiftCardTransactionDto MapTransactionToDto(GiftCardTransaction transaction)
    {
        return new GiftCardTransactionDto
        {
            Id = transaction.Id,
            GiftCardId = transaction.GiftCardId,
            GiftCardCode = transaction.GiftCard?.Code ?? string.Empty,
            MaskedGiftCardCode = MaskCode(transaction.GiftCard?.Code ?? string.Empty),
            OrderId = transaction.OrderId,
            OrderNumber = transaction.Order?.OrderNumber,
            Type = transaction.Type,
            Amount = transaction.Amount,
            BalanceAfter = transaction.BalanceAfter,
            Description = transaction.Description,
            CreatedAt = transaction.CreatedAt
        };
    }

    private static string MaskCode(string code)
    {
        if (string.IsNullOrWhiteSpace(code) || code.Length <= 4)
        {
            return code;
        }

        return $"{new string('*', Math.Max(0, code.Length - 4))}{code[^4..]}";
    }
}
