using EcommerceAPI.Business.Abstract;
using EcommerceAPI.Core.CrossCuttingConcerns.Logging;
using EcommerceAPI.Core.Interfaces;
using EcommerceAPI.Core.Utilities.Results;
using EcommerceAPI.DataAccess.Abstract;
using EcommerceAPI.Entities.Concrete;
using EcommerceAPI.Entities.DTOs;
using EcommerceAPI.Entities.Enums;

namespace EcommerceAPI.Business.Concrete;

public class LoyaltyManager : ILoyaltyService
{
    private const int PointsPerLiraDiscount = 100;
    private readonly ILoyaltyTransactionDal _loyaltyTransactionDal;
    private readonly IOrderDal _orderDal;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditService _auditService;

    public LoyaltyManager(
        ILoyaltyTransactionDal loyaltyTransactionDal,
        IOrderDal orderDal,
        IUnitOfWork unitOfWork,
        IAuditService auditService)
    {
        _loyaltyTransactionDal = loyaltyTransactionDal;
        _orderDal = orderDal;
        _unitOfWork = unitOfWork;
        _auditService = auditService;
    }

    public async Task<IDataResult<LoyaltySummaryDto>> GetSummaryAsync(int userId, int recentLimit = 10)
    {
        var availablePoints = await _loyaltyTransactionDal.GetAvailablePointsAsync(userId, DateTime.UtcNow);
        var totalEarned = await _loyaltyTransactionDal.GetTotalPointsByTypeAsync(userId, LoyaltyTransactionType.Earned);
        var totalRedeemed = Math.Abs(await _loyaltyTransactionDal.GetTotalPointsByTypeAsync(userId, LoyaltyTransactionType.Redeemed));
        var transactions = await _loyaltyTransactionDal.GetUserTransactionsAsync(userId, recentLimit);

        return new SuccessDataResult<LoyaltySummaryDto>(new LoyaltySummaryDto
        {
            AvailablePoints = availablePoints,
            AvailableDiscountAmount = Math.Max(0m, ConvertPointsToDiscount(availablePoints)),
            TotalEarnedPoints = totalEarned,
            TotalRedeemedPoints = totalRedeemed,
            PointsPerLira = PointsPerLiraDiscount,
            RecentTransactions = transactions.Select(MapToDto).ToList()
        });
    }

    public async Task<IDataResult<List<LoyaltyTransactionDto>>> GetHistoryAsync(int userId, int limit = 50)
    {
        var transactions = await _loyaltyTransactionDal.GetUserTransactionsAsync(userId, limit);
        return new SuccessDataResult<List<LoyaltyTransactionDto>>(transactions.Select(MapToDto).ToList());
    }

    public async Task<IDataResult<LoyaltyRedemptionPreviewDto>> CalculateRedemptionAsync(int userId, int requestedPoints, decimal orderTotal)
    {
        if (requestedPoints <= 0)
        {
            return new SuccessDataResult<LoyaltyRedemptionPreviewDto>(new LoyaltyRedemptionPreviewDto());
        }

        var availablePoints = await _loyaltyTransactionDal.GetAvailablePointsAsync(userId, DateTime.UtcNow);
        if (availablePoints <= 0)
        {
            return new ErrorDataResult<LoyaltyRedemptionPreviewDto>("Kullanılabilir sadakat puanınız bulunmuyor.");
        }

        if (orderTotal <= 1m)
        {
            return new ErrorDataResult<LoyaltyRedemptionPreviewDto>("Puan kullanımı için sipariş tutarı yeterli değil.");
        }

        var normalizedRequestedPoints = NormalizePoints(requestedPoints);
        if (normalizedRequestedPoints <= 0)
        {
            return new ErrorDataResult<LoyaltyRedemptionPreviewDto>($"Puan kullanımı {PointsPerLiraDiscount} puanlık adımlarla yapılır.");
        }

        var maxDiscount = Math.Max(0m, orderTotal - 1m);
        var maxPointsByTotal = (int)Math.Floor(maxDiscount * PointsPerLiraDiscount);
        maxPointsByTotal = NormalizePoints(maxPointsByTotal);

        var appliedPoints = Math.Min(normalizedRequestedPoints, Math.Min(availablePoints, maxPointsByTotal));
        if (appliedPoints <= 0)
        {
            return new ErrorDataResult<LoyaltyRedemptionPreviewDto>("Bu siparişte kullanılabilecek sadakat puanı bulunamadı.");
        }

        return new SuccessDataResult<LoyaltyRedemptionPreviewDto>(new LoyaltyRedemptionPreviewDto
        {
            RequestedPoints = requestedPoints,
            AppliedPoints = appliedPoints,
            AvailablePoints = availablePoints,
            DiscountAmount = ConvertPointsToDiscount(appliedPoints)
        });
    }

    public async Task<IResult> RedeemPointsForOrderAsync(int userId, int orderId, int points, decimal discountAmount, string description)
    {
        if (points <= 0)
        {
            return new SuccessResult();
        }

        var existing = await _loyaltyTransactionDal.GetByOrderAndTypeAsync(orderId, LoyaltyTransactionType.Redeemed);
        if (existing != null)
        {
            return new SuccessResult("Puan kullanımı zaten işlendi.");
        }

        var order = await _orderDal.GetByIdWithDetailsAsync(orderId);
        if (order == null || order.UserId != userId)
        {
            return new ErrorResult("Sipariş bulunamadı.");
        }

        var availablePoints = await _loyaltyTransactionDal.GetAvailablePointsAsync(userId, DateTime.UtcNow);
        if (availablePoints < points)
        {
            return new ErrorResult("Sadakat puanı bakiyesi yetersiz.");
        }

        var balanceAfter = availablePoints - points;
        await _loyaltyTransactionDal.AddAsync(new LoyaltyTransaction
        {
            UserId = userId,
            OrderId = orderId,
            Type = LoyaltyTransactionType.Redeemed,
            Points = -points,
            BalanceAfter = balanceAfter,
            Description = description
        });

        order.LoyaltyPointsUsed = points;
        order.LoyaltyDiscountAmount = discountAmount;
        _orderDal.Update(order);

        await _auditService.LogActionAsync(
            userId.ToString(),
            "RedeemLoyaltyPoints",
            "LoyaltyTransaction",
            new { OrderId = orderId, Points = points, DiscountAmount = discountAmount });

        return new SuccessResult();
    }

    public async Task<IResult> RestoreRedeemedPointsAsync(int userId, int orderId, string description)
    {
        var redeemed = await _loyaltyTransactionDal.GetByOrderAndTypeAsync(orderId, LoyaltyTransactionType.Redeemed);
        if (redeemed == null)
        {
            return new SuccessResult();
        }

        var restored = await _loyaltyTransactionDal.GetByOrderAndTypeAsync(orderId, LoyaltyTransactionType.Restored);
        if (restored != null)
        {
            return new SuccessResult("Puan iadesi zaten işlendi.");
        }

        var availablePoints = await _loyaltyTransactionDal.GetAvailablePointsAsync(userId, DateTime.UtcNow);
        var restoredPoints = Math.Abs(redeemed.Points);

        await _loyaltyTransactionDal.AddAsync(new LoyaltyTransaction
        {
            UserId = userId,
            OrderId = orderId,
            Type = LoyaltyTransactionType.Restored,
            Points = restoredPoints,
            BalanceAfter = availablePoints + restoredPoints,
            Description = description
        });

        await _auditService.LogActionAsync(
            userId.ToString(),
            "RestoreLoyaltyPoints",
            "LoyaltyTransaction",
            new { OrderId = orderId, Points = restoredPoints });

        return new SuccessResult();
    }

    public async Task<IResult> AwardPointsForOrderAsync(int userId, int orderId, decimal paidAmount)
    {
        var existing = await _loyaltyTransactionDal.GetByOrderAndTypeAsync(orderId, LoyaltyTransactionType.Earned);
        if (existing != null)
        {
            return new SuccessResult("Sadakat puanı zaten kazanıldı.");
        }

        var order = await _orderDal.GetByIdWithDetailsAsync(orderId);
        if (order == null || order.UserId != userId)
        {
            return new ErrorResult("Sipariş bulunamadı.");
        }

        var pointsToAward = (int)Math.Floor(Math.Max(0m, paidAmount));
        order.LoyaltyPointsEarned = pointsToAward;
        _orderDal.Update(order);

        if (pointsToAward <= 0)
        {
            return new SuccessResult();
        }

        var availablePoints = await _loyaltyTransactionDal.GetAvailablePointsAsync(userId, DateTime.UtcNow);

        await _loyaltyTransactionDal.AddAsync(new LoyaltyTransaction
        {
            UserId = userId,
            OrderId = orderId,
            Type = LoyaltyTransactionType.Earned,
            Points = pointsToAward,
            BalanceAfter = availablePoints + pointsToAward,
            Description = $"Sipariş ödemesi sonrası kazanım ({order.OrderNumber})"
        });

        await _auditService.LogActionAsync(
            userId.ToString(),
            "AwardLoyaltyPoints",
            "LoyaltyTransaction",
            new { OrderId = orderId, Points = pointsToAward, PaidAmount = paidAmount });

        return new SuccessResult();
    }

    public async Task<IResult> ReverseEarnedPointsAsync(int userId, int orderId, string description)
    {
        var earned = await _loyaltyTransactionDal.GetByOrderAndTypeAsync(orderId, LoyaltyTransactionType.Earned);
        if (earned == null || earned.Points <= 0)
        {
            return new SuccessResult();
        }

        var reversed = await _loyaltyTransactionDal.GetByOrderAndTypeAsync(orderId, LoyaltyTransactionType.Reversed);
        if (reversed != null)
        {
            return new SuccessResult("Kazanılan puanların geri alınması zaten işlendi.");
        }

        var availablePoints = await _loyaltyTransactionDal.GetAvailablePointsAsync(userId, DateTime.UtcNow);
        var pointsToReverse = earned.Points;

        await _loyaltyTransactionDal.AddAsync(new LoyaltyTransaction
        {
            UserId = userId,
            OrderId = orderId,
            Type = LoyaltyTransactionType.Reversed,
            Points = -pointsToReverse,
            BalanceAfter = availablePoints - pointsToReverse,
            Description = description
        });

        await _auditService.LogActionAsync(
            userId.ToString(),
            "ReverseLoyaltyPoints",
            "LoyaltyTransaction",
            new { OrderId = orderId, Points = pointsToReverse });

        return new SuccessResult();
    }

    private static int NormalizePoints(int points)
    {
        if (points <= 0)
        {
            return 0;
        }

        return points - (points % PointsPerLiraDiscount);
    }

    private static decimal ConvertPointsToDiscount(int points)
    {
        return points / (decimal)PointsPerLiraDiscount;
    }

    private static LoyaltyTransactionDto MapToDto(LoyaltyTransaction transaction)
    {
        return new LoyaltyTransactionDto
        {
            Id = transaction.Id,
            OrderId = transaction.OrderId,
            OrderNumber = transaction.Order?.OrderNumber,
            Type = transaction.Type.ToString(),
            Points = transaction.Points,
            BalanceAfter = transaction.BalanceAfter,
            Description = transaction.Description,
            ExpiresAt = transaction.ExpiresAt,
            CreatedAt = transaction.CreatedAt
        };
    }
}
