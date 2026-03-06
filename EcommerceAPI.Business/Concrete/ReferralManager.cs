using System.Security.Cryptography;
using EcommerceAPI.Application.Abstractions.ServiceContracts;
using EcommerceAPI.Core.CrossCuttingConcerns.Logging;
using EcommerceAPI.Core.Interfaces;
using EcommerceAPI.Core.Utilities.Results;
using EcommerceAPI.Application.Abstractions.Persistence;
using EcommerceAPI.Entities.Concrete;
using EcommerceAPI.Entities.DTOs;
using EcommerceAPI.Entities.Enums;

namespace EcommerceAPI.Business.Concrete;

public class ReferralManager : IReferralService
{
    private const int ReferrerRewardPoints = 500;
    private const int ReferredRewardPoints = 250;

    private readonly IReferralCodeDal _referralCodeDal;
    private readonly IReferralTransactionDal _referralTransactionDal;
    private readonly IUserDal _userDal;
    private readonly IOrderDal _orderDal;
    private readonly ILoyaltyTransactionDal _loyaltyTransactionDal;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditService _auditService;

    public ReferralManager(
        IReferralCodeDal referralCodeDal,
        IReferralTransactionDal referralTransactionDal,
        IUserDal userDal,
        IOrderDal orderDal,
        ILoyaltyTransactionDal loyaltyTransactionDal,
        IUnitOfWork unitOfWork,
        IAuditService auditService)
    {
        _referralCodeDal = referralCodeDal;
        _referralTransactionDal = referralTransactionDal;
        _userDal = userDal;
        _orderDal = orderDal;
        _loyaltyTransactionDal = loyaltyTransactionDal;
        _unitOfWork = unitOfWork;
        _auditService = auditService;
    }

    public async Task<IDataResult<ReferralSummaryDto>> GetSummaryAsync(int userId, int recentLimit = 10)
    {
        var ensured = await EnsureReferralCodeExistsAsync(userId);
        if (!ensured.Success)
        {
            return new ErrorDataResult<ReferralSummaryDto>(ensured.Message);
        }

        if (ensured.Changed)
        {
            await _unitOfWork.SaveChangesAsync();
        }

        var user = await _userDal.GetAsync(x => x.Id == userId);
        if (user == null)
        {
            return new ErrorDataResult<ReferralSummaryDto>("Kullanıcı bulunamadı.");
        }

        var referralCode = await _referralCodeDal.GetByUserIdAsync(userId);
        if (referralCode == null)
        {
            return new ErrorDataResult<ReferralSummaryDto>("Referral kodu bulunamadı.");
        }

        var totalReferrals = await _userDal.CountAsync(x => x.ReferredByUserId == userId);
        var successfulReferrals = await _userDal.CountAsync(x => x.ReferredByUserId == userId && x.ReferralRewardedOrderId.HasValue);
        var totalRewardPoints = await _referralTransactionDal.GetTotalRewardPointsAsync(userId);
        var referredByCode = user.AppliedReferralCodeId.HasValue
            ? (await _referralCodeDal.GetAsync(x => x.Id == user.AppliedReferralCodeId.Value))?.Code
            : null;
        var recentTransactions = await _referralTransactionDal.GetUserTransactionsAsync(userId, recentLimit);

        return new SuccessDataResult<ReferralSummaryDto>(new ReferralSummaryDto
        {
            ReferralCode = referralCode.Code,
            TotalReferrals = totalReferrals,
            SuccessfulReferrals = successfulReferrals,
            PendingReferrals = Math.Max(0, totalReferrals - successfulReferrals),
            TotalRewardPoints = totalRewardPoints,
            ReferrerRewardPoints = ReferrerRewardPoints,
            ReferredRewardPoints = ReferredRewardPoints,
            ReferredByCode = referredByCode,
            RecentTransactions = recentTransactions.Select(x => MapTransaction(x, userId)).ToList()
        });
    }

    public async Task<IDataResult<List<ReferralTransactionDto>>> GetHistoryAsync(int userId, int limit = 50)
    {
        var ensured = await EnsureReferralCodeExistsAsync(userId);
        if (!ensured.Success)
        {
            return new ErrorDataResult<List<ReferralTransactionDto>>(ensured.Message);
        }

        if (ensured.Changed)
        {
            await _unitOfWork.SaveChangesAsync();
        }

        var transactions = await _referralTransactionDal.GetUserTransactionsAsync(userId, limit);
        return new SuccessDataResult<List<ReferralTransactionDto>>(transactions.Select(x => MapTransaction(x, userId)).ToList());
    }

    public async Task<IResult> ValidateReferralCodeAsync(string? referralCode)
    {
        if (string.IsNullOrWhiteSpace(referralCode))
        {
            return new SuccessResult();
        }

        var normalizedCode = referralCode.Trim().ToUpperInvariant();
        var matchedCode = await _referralCodeDal.GetByCodeAsync(normalizedCode);
        if (matchedCode == null || !matchedCode.IsActive)
        {
            return new ErrorResult("Geçerli bir referral kodu bulunamadı.");
        }

        return new SuccessResult();
    }

    public async Task<IResult> SetupNewUserAsync(int userId, string? referralCode = null)
    {
        var user = await _userDal.GetAsync(x => x.Id == userId);
        if (user == null)
        {
            return new ErrorResult("Kullanıcı bulunamadı.");
        }

        var ensured = await EnsureReferralCodeExistsAsync(userId);
        if (!ensured.Success)
        {
            return new ErrorResult(ensured.Message);
        }

        if (string.IsNullOrWhiteSpace(referralCode) || user.ReferredByUserId.HasValue)
        {
            return new SuccessResult();
        }

        var normalizedCode = referralCode.Trim().ToUpperInvariant();
        var matchedCode = await _referralCodeDal.GetByCodeAsync(normalizedCode);
        if (matchedCode == null || !matchedCode.IsActive)
        {
            return new ErrorResult("Geçerli bir referral kodu bulunamadı.");
        }

        if (matchedCode.UserId == userId)
        {
            return new ErrorResult("Kendi referral kodunu kullanamazsın.");
        }

        user.ReferredByUserId = matchedCode.UserId;
        user.AppliedReferralCodeId = matchedCode.Id;
        _userDal.Update(user);

        var hasSignupTransaction = await _referralTransactionDal.ExistsAsync(
            x => x.ReferredUserId == userId && x.Type == ReferralTransactionType.Signup);

        if (!hasSignupTransaction)
        {
            await _referralTransactionDal.AddAsync(new ReferralTransaction
            {
                ReferralCodeId = matchedCode.Id,
                ReferrerUserId = matchedCode.UserId,
                ReferredUserId = userId,
                Type = ReferralTransactionType.Signup,
                Points = 0,
                Description = $"Referral kaydı oluşturuldu ({matchedCode.Code})"
            });
        }

        return new SuccessResult();
    }

    public async Task<IResult> AwardFirstPurchaseRewardsAsync(int orderId)
    {
        var order = await _orderDal.GetByIdWithDetailsAsync(orderId);
        if (order == null)
        {
            return new ErrorResult("Sipariş bulunamadı.");
        }

        if (order.Status != OrderStatus.Paid || order.Payment?.Status != PaymentStatus.Success)
        {
            return new SuccessResult();
        }

        var user = await _userDal.GetAsync(x => x.Id == order.UserId);
        if (user == null || !user.ReferredByUserId.HasValue || !user.AppliedReferralCodeId.HasValue)
        {
            return new SuccessResult();
        }

        if (user.ReferralRewardedOrderId.HasValue)
        {
            return new SuccessResult("Referral ödülü zaten işlendi.");
        }

        var previousPaidOrderCount = await _orderDal.CountAsync(
            x => x.UserId == order.UserId && x.Id != order.Id && x.Status == OrderStatus.Paid);

        if (previousPaidOrderCount > 0)
        {
            return new SuccessResult();
        }

        if (await _referralTransactionDal.ExistsAsync(
                x => x.OrderId == order.Id &&
                     (x.Type == ReferralTransactionType.ReferrerRewardGranted ||
                      x.Type == ReferralTransactionType.ReferredRewardGranted)))
        {
            return new SuccessResult("Referral ödülü zaten işlendi.");
        }

        var referralCode = await _referralCodeDal.GetAsync(x => x.Id == user.AppliedReferralCodeId.Value);
        if (referralCode == null)
        {
            return new ErrorResult("Referral kodu bulunamadı.");
        }

        await AwardLoyaltyBonusAsync(
            user.ReferredByUserId.Value,
            ReferrerRewardPoints,
            $"Referral ödülü kazanımı ({order.OrderNumber})");

        await AwardLoyaltyBonusAsync(
            user.Id,
            ReferredRewardPoints,
            $"İlk sipariş referral hoş geldin ödülü ({order.OrderNumber})");

        await _referralTransactionDal.AddRangeAsync(
        [
            new ReferralTransaction
            {
                ReferralCodeId = referralCode.Id,
                ReferrerUserId = user.ReferredByUserId.Value,
                ReferredUserId = user.Id,
                BeneficiaryUserId = user.ReferredByUserId.Value,
                OrderId = order.Id,
                Type = ReferralTransactionType.ReferrerRewardGranted,
                Points = ReferrerRewardPoints,
                Description = $"Referrer ödülü işlendi ({order.OrderNumber})"
            },
            new ReferralTransaction
            {
                ReferralCodeId = referralCode.Id,
                ReferrerUserId = user.ReferredByUserId.Value,
                ReferredUserId = user.Id,
                BeneficiaryUserId = user.Id,
                OrderId = order.Id,
                Type = ReferralTransactionType.ReferredRewardGranted,
                Points = ReferredRewardPoints,
                Description = $"Referred kullanıcı ödülü işlendi ({order.OrderNumber})"
            }
        ]);

        user.ReferralRewardedOrderId = order.Id;
        _userDal.Update(user);

        await _auditService.LogActionAsync(
            user.Id.ToString(),
            "AwardReferralRewards",
            "ReferralTransaction",
            new { order.Id, ReferrerUserId = user.ReferredByUserId.Value, ReferredUserId = user.Id });

        return new SuccessResult();
    }

    public async Task<IResult> ReverseRewardsForOrderAsync(int orderId, string description)
    {
        var grantedTransactions = await _referralTransactionDal.GetGrantedTransactionsByOrderAsync(orderId);
        if (!grantedTransactions.Any())
        {
            return new SuccessResult();
        }

        foreach (var grantedTransaction in grantedTransactions)
        {
            if (!grantedTransaction.BeneficiaryUserId.HasValue)
            {
                continue;
            }

            var reverseType = grantedTransaction.Type == ReferralTransactionType.ReferrerRewardGranted
                ? ReferralTransactionType.ReferrerRewardReversed
                : ReferralTransactionType.ReferredRewardReversed;

            var alreadyReversed = await _referralTransactionDal.ExistsAsync(
                x => x.OrderId == orderId &&
                     x.BeneficiaryUserId == grantedTransaction.BeneficiaryUserId &&
                     x.Type == reverseType);

            if (alreadyReversed)
            {
                continue;
            }

            var pointsToReverse = Math.Abs(grantedTransaction.Points);
            await ReverseLoyaltyBonusAsync(grantedTransaction.BeneficiaryUserId.Value, pointsToReverse, description);

            await _referralTransactionDal.AddAsync(new ReferralTransaction
            {
                ReferralCodeId = grantedTransaction.ReferralCodeId,
                ReferrerUserId = grantedTransaction.ReferrerUserId,
                ReferredUserId = grantedTransaction.ReferredUserId,
                BeneficiaryUserId = grantedTransaction.BeneficiaryUserId,
                OrderId = orderId,
                Type = reverseType,
                Points = -pointsToReverse,
                Description = description
            });
        }

        var orderUser = await _userDal.GetAsync(x => x.Id == grantedTransactions.First().ReferredUserId);
        if (orderUser != null && orderUser.ReferralRewardedOrderId == orderId)
        {
            orderUser.ReferralRewardedOrderId = null;
            _userDal.Update(orderUser);
        }

        await _auditService.LogActionAsync(
            grantedTransactions.First().ReferredUserId.ToString(),
            "ReverseReferralRewards",
            "ReferralTransaction",
            new { OrderId = orderId });

        return new SuccessResult();
    }

    private async Task<(bool Success, bool Changed, string Message)> EnsureReferralCodeExistsAsync(int userId)
    {
        var existingCode = await _referralCodeDal.GetByUserIdAsync(userId);
        if (existingCode != null)
        {
            return (true, false, string.Empty);
        }

        var userExists = await _userDal.ExistsAsync(x => x.Id == userId);
        if (!userExists)
        {
            return (false, false, "Kullanıcı bulunamadı.");
        }

        await _referralCodeDal.AddAsync(new ReferralCode
        {
            UserId = userId,
            Code = await GenerateUniqueCodeAsync(),
            IsActive = true
        });

        return (true, true, string.Empty);
    }

    private async Task<string> GenerateUniqueCodeAsync()
    {
        while (true)
        {
            var candidate = $"REF{GetRandomToken(8)}";
            var exists = await _referralCodeDal.ExistsAsync(x => x.Code == candidate);
            if (!exists)
            {
                return candidate;
            }
        }
    }

    private static string GetRandomToken(int length)
    {
        const string alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        Span<char> chars = stackalloc char[length];
        Span<byte> buffer = stackalloc byte[length];
        RandomNumberGenerator.Fill(buffer);

        for (var i = 0; i < length; i++)
        {
            chars[i] = alphabet[buffer[i] % alphabet.Length];
        }

        return new string(chars);
    }

    private async Task AwardLoyaltyBonusAsync(int userId, int points, string description)
    {
        var availablePoints = await _loyaltyTransactionDal.GetAvailablePointsAsync(userId, DateTime.UtcNow);
        await _loyaltyTransactionDal.AddAsync(new LoyaltyTransaction
        {
            UserId = userId,
            Type = LoyaltyTransactionType.Earned,
            Points = points,
            BalanceAfter = availablePoints + points,
            Description = description
        });
    }

    private async Task ReverseLoyaltyBonusAsync(int userId, int points, string description)
    {
        var availablePoints = await _loyaltyTransactionDal.GetAvailablePointsAsync(userId, DateTime.UtcNow);
        await _loyaltyTransactionDal.AddAsync(new LoyaltyTransaction
        {
            UserId = userId,
            Type = LoyaltyTransactionType.Reversed,
            Points = -points,
            BalanceAfter = availablePoints - points,
            Description = description
        });
    }

    private static ReferralTransactionDto MapTransaction(ReferralTransaction transaction, int currentUserId)
    {
        return new ReferralTransactionDto
        {
            Id = transaction.Id,
            Type = transaction.Type.ToString(),
            Points = transaction.Points,
            OrderId = transaction.OrderId,
            OrderNumber = transaction.Order?.OrderNumber,
            Description = transaction.Description,
            RelatedUserName = ResolveRelatedUserName(transaction, currentUserId),
            CreatedAt = transaction.CreatedAt
        };
    }

    private static string? ResolveRelatedUserName(ReferralTransaction transaction, int currentUserId)
    {
        if (transaction.ReferrerUserId == currentUserId)
        {
            return $"{transaction.ReferredUser.FirstName} {transaction.ReferredUser.LastName}".Trim();
        }

        if (transaction.ReferredUserId == currentUserId)
        {
            return $"{transaction.ReferrerUser.FirstName} {transaction.ReferrerUser.LastName}".Trim();
        }

        if (transaction.BeneficiaryUserId == currentUserId && transaction.ReferrerUserId != currentUserId)
        {
            return $"{transaction.ReferrerUser.FirstName} {transaction.ReferrerUser.LastName}".Trim();
        }

        return null;
    }
}
