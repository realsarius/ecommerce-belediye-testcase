using EcommerceAPI.Business.Abstract;
using EcommerceAPI.Core.Utilities.Results;
using EcommerceAPI.DataAccess.Abstract;
using EcommerceAPI.Entities.Concrete;
using EcommerceAPI.Entities.DTOs;
using EcommerceAPI.Entities.Enums;
using EcommerceAPI.Core.Interfaces;
using EcommerceAPI.Core.CrossCuttingConcerns.Logging;
using EcommerceAPI.Core.Aspects.Autofac.Caching;
using EcommerceAPI.Core.Aspects.Autofac.Logging;
using EcommerceAPI.Business.Constants;

namespace EcommerceAPI.Business.Concrete;

public class CouponManager : ICouponService
{
    private readonly ICouponDal _couponDal;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditService _auditService;

    public CouponManager(ICouponDal couponDal, IUnitOfWork unitOfWork, IAuditService auditService)
    {
        _couponDal = couponDal;
        _unitOfWork = unitOfWork;
        _auditService = auditService;
    }

    [LogAspect]
    [CacheAspect(duration: 60)]
    public async Task<IDataResult<List<CouponDto>>> GetAllAsync()
    {
        var coupons = await _couponDal.GetListAsync();
        var couponDtos = coupons.Select(MapToDto).ToList();
        return new SuccessDataResult<List<CouponDto>>(couponDtos);
    }

    [LogAspect]
    public async Task<IDataResult<CouponDto>> GetByIdAsync(int id)
    {
        var coupon = await _couponDal.GetAsync(c => c.Id == id);
        if (coupon == null)
            return new ErrorDataResult<CouponDto>(Messages.CouponNotFound);
        
        return new SuccessDataResult<CouponDto>(MapToDto(coupon));
    }

    [LogAspect]
    [CacheRemoveAspect("GetAllAsync")]
    public async Task<IDataResult<CouponDto>> CreateAsync(CreateCouponRequest request)
    {

        var existing = await _couponDal.GetByCodeAsync(request.Code);
        if (existing != null)
            return new ErrorDataResult<CouponDto>(Messages.CouponAlreadyExists);

        var coupon = new Coupon
        {
            Code = request.Code.ToUpper().Trim(),
            Type = request.Type,
            Value = request.Value,
            MinOrderAmount = request.MinOrderAmount,
            UsageLimit = request.UsageLimit,
            UsedCount = 0,
            ExpiresAt = DateTime.UtcNow.AddDays(request.ValidDays),
            IsActive = true,
            Description = request.Description
        };

        await _couponDal.AddAsync(coupon);
        await _unitOfWork.SaveChangesAsync();
        
        await _auditService.LogActionAsync(
            "Admin",
            "CreateCoupon",
            "Coupon",
            new { CouponId = coupon.Id, Code = coupon.Code, Type = coupon.Type.ToString(), Value = coupon.Value });
        
        return new SuccessDataResult<CouponDto>(MapToDto(coupon), Messages.CouponCreated);
    }

    [LogAspect]
    [CacheRemoveAspect("GetAllAsync")]
    public async Task<IDataResult<CouponDto>> UpdateAsync(int id, UpdateCouponRequest request)
    {
        var coupon = await _couponDal.GetAsync(c => c.Id == id);
        if (coupon == null)
            return new ErrorDataResult<CouponDto>(Messages.CouponNotFound);


        if (!string.IsNullOrEmpty(request.Code) && request.Code.ToUpper() != coupon.Code)
        {
            var existing = await _couponDal.GetByCodeAsync(request.Code);
            if (existing != null)
                return new ErrorDataResult<CouponDto>(Messages.CouponAlreadyExists);
            coupon.Code = request.Code.ToUpper().Trim();
        }

        if (request.Type.HasValue) coupon.Type = request.Type.Value;
        if (request.Value.HasValue) coupon.Value = request.Value.Value;
        if (request.MinOrderAmount.HasValue) coupon.MinOrderAmount = request.MinOrderAmount;
        if (request.UsageLimit.HasValue) coupon.UsageLimit = request.UsageLimit.Value;
        if (request.ExpiresAt.HasValue) coupon.ExpiresAt = request.ExpiresAt.Value;
        if (request.IsActive.HasValue) coupon.IsActive = request.IsActive.Value;
        if (request.Description != null) coupon.Description = request.Description;

        coupon.UpdatedAt = DateTime.UtcNow;
        _couponDal.Update(coupon);
        await _unitOfWork.SaveChangesAsync();
        
        await _auditService.LogActionAsync(
            "Admin",
            "UpdateCoupon",
            "Coupon",
            new { CouponId = coupon.Id, Code = coupon.Code });
        
        return new SuccessDataResult<CouponDto>(MapToDto(coupon), Messages.CouponUpdated);
    }

    [LogAspect]
    [CacheRemoveAspect("GetAllAsync")]
    public async Task<IResult> DeleteAsync(int id)
    {
        var coupon = await _couponDal.GetAsync(c => c.Id == id);
        if (coupon == null)
            return new ErrorResult(Messages.CouponNotFound);

        _couponDal.Delete(coupon);
        await _unitOfWork.SaveChangesAsync();
        
        await _auditService.LogActionAsync(
            "Admin",
            "DeleteCoupon",
            "Coupon",
            new { CouponId = coupon.Id, Code = coupon.Code });
        
        return new SuccessResult(Messages.CouponDeleted);
    }

    [LogAspect]
    public async Task<IDataResult<CouponValidationResult>> ValidateCouponAsync(string code, decimal orderTotal)
    {
        var result = new CouponValidationResult
        {
            IsValid = false,
            FinalTotal = orderTotal
        };

        var coupon = await _couponDal.GetByCodeAsync(code);
        if (coupon == null)
        {
            result.ErrorMessage = Messages.CouponNotFound;
            return new SuccessDataResult<CouponValidationResult>(result);
        }

        if (!coupon.IsActive)
        {
            result.ErrorMessage = Messages.CouponInactive;
            return new SuccessDataResult<CouponValidationResult>(result);
        }

        if (coupon.ExpiresAt <= DateTime.UtcNow)
        {
            result.ErrorMessage = Messages.CouponExpired;
            return new SuccessDataResult<CouponValidationResult>(result);
        }

        if (coupon.UsageLimit > 0 && coupon.UsedCount >= coupon.UsageLimit)
        {
            result.ErrorMessage = Messages.CouponUsageLimitExceeded;
            return new SuccessDataResult<CouponValidationResult>(result);
        }

        if (coupon.MinOrderAmount.HasValue && orderTotal < coupon.MinOrderAmount.Value)
        {
            result.ErrorMessage = $"{Messages.CouponMinAmountNotMet} ({coupon.MinOrderAmount.Value:N2} TL)";
            return new SuccessDataResult<CouponValidationResult>(result);
        }

        decimal discountAmount = coupon.Type switch
        {
            CouponType.Percentage => Math.Round(orderTotal * (coupon.Value / 100), 2),
            CouponType.FixedAmount => Math.Min(coupon.Value, orderTotal),
            _ => 0
        };

        result.IsValid = true;
        result.Coupon = MapToDto(coupon);
        result.DiscountAmount = discountAmount;
        result.FinalTotal = orderTotal - discountAmount;

        return new SuccessDataResult<CouponValidationResult>(result);
    }

    [LogAspect]
    public async Task<IResult> IncrementUsageAsync(int couponId)
    {
        var coupon = await _couponDal.GetAsync(c => c.Id == couponId);
        if (coupon == null)
            return new ErrorResult(Messages.CouponNotFound);

        coupon.UsedCount++;
        coupon.UpdatedAt = DateTime.UtcNow;
        _couponDal.Update(coupon);
        await _unitOfWork.SaveChangesAsync();
        
        await _auditService.LogActionAsync(
            "System",
            "UseCoupon",
            "Coupon",
            new { CouponId = coupon.Id, Code = coupon.Code, UsedCount = coupon.UsedCount });
        
        return new SuccessResult();
    }

    private static CouponDto MapToDto(Coupon coupon)
    {
        return new CouponDto
        {
            Id = coupon.Id,
            Code = coupon.Code,
            Type = coupon.Type,
            Value = coupon.Value,
            MinOrderAmount = coupon.MinOrderAmount,
            UsageLimit = coupon.UsageLimit,
            UsedCount = coupon.UsedCount,
            ExpiresAt = coupon.ExpiresAt,
            IsActive = coupon.IsActive,
            Description = coupon.Description,
            CreatedAt = coupon.CreatedAt
        };
    }
}
