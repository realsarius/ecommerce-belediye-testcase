using EcommerceAPI.Business.Abstract;
using EcommerceAPI.Core.Utilities.Results;
using EcommerceAPI.DataAccess.Abstract;
using EcommerceAPI.Entities.Concrete;
using EcommerceAPI.Entities.DTOs;
using EcommerceAPI.Entities.Enums;
using EcommerceAPI.Core.Interfaces;
using EcommerceAPI.Core.CrossCuttingConcerns.Logging;

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

    public async Task<IDataResult<List<CouponDto>>> GetAllAsync()
    {
        var coupons = await _couponDal.GetListAsync();
        var couponDtos = coupons.Select(MapToDto).ToList();
        return new SuccessDataResult<List<CouponDto>>(couponDtos);
    }

    public async Task<IDataResult<CouponDto>> GetByIdAsync(int id)
    {
        var coupon = await _couponDal.GetAsync(c => c.Id == id);
        if (coupon == null)
            return new ErrorDataResult<CouponDto>("Kupon bulunamadı.");
        
        return new SuccessDataResult<CouponDto>(MapToDto(coupon));
    }

    public async Task<IDataResult<CouponDto>> CreateAsync(CreateCouponRequest request)
    {

        var existing = await _couponDal.GetByCodeAsync(request.Code);
        if (existing != null)
            return new ErrorDataResult<CouponDto>("Bu kupon kodu zaten kullanılıyor.");

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
        
        return new SuccessDataResult<CouponDto>(MapToDto(coupon), "Kupon başarıyla oluşturuldu.");
    }

    public async Task<IDataResult<CouponDto>> UpdateAsync(int id, UpdateCouponRequest request)
    {
        var coupon = await _couponDal.GetAsync(c => c.Id == id);
        if (coupon == null)
            return new ErrorDataResult<CouponDto>("Kupon bulunamadı.");


        if (!string.IsNullOrEmpty(request.Code) && request.Code.ToUpper() != coupon.Code)
        {
            var existing = await _couponDal.GetByCodeAsync(request.Code);
            if (existing != null)
                return new ErrorDataResult<CouponDto>("Bu kupon kodu zaten kullanılıyor.");
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
        
        return new SuccessDataResult<CouponDto>(MapToDto(coupon), "Kupon başarıyla güncellendi.");
    }

    public async Task<IResult> DeleteAsync(int id)
    {
        var coupon = await _couponDal.GetAsync(c => c.Id == id);
        if (coupon == null)
            return new ErrorResult("Kupon bulunamadı.");

        _couponDal.Delete(coupon);
        await _unitOfWork.SaveChangesAsync();
        
        await _auditService.LogActionAsync(
            "Admin",
            "DeleteCoupon",
            "Coupon",
            new { CouponId = coupon.Id, Code = coupon.Code });
        
        return new SuccessResult("Kupon başarıyla silindi.");
    }

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
            result.ErrorMessage = "Kupon kodu bulunamadı.";
            return new SuccessDataResult<CouponValidationResult>(result);
        }

        if (!coupon.IsActive)
        {
            result.ErrorMessage = "Bu kupon aktif değil.";
            return new SuccessDataResult<CouponValidationResult>(result);
        }

        if (coupon.ExpiresAt <= DateTime.UtcNow)
        {
            result.ErrorMessage = "Bu kuponun süresi dolmuş.";
            return new SuccessDataResult<CouponValidationResult>(result);
        }

        if (coupon.UsageLimit > 0 && coupon.UsedCount >= coupon.UsageLimit)
        {
            result.ErrorMessage = "Bu kuponun kullanım limiti dolmuş.";
            return new SuccessDataResult<CouponValidationResult>(result);
        }

        if (coupon.MinOrderAmount.HasValue && orderTotal < coupon.MinOrderAmount.Value)
        {
            result.ErrorMessage = $"Bu kupon için minimum sipariş tutarı {coupon.MinOrderAmount.Value:N2} TL'dir.";
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

    public async Task<IResult> IncrementUsageAsync(int couponId)
    {
        var coupon = await _couponDal.GetAsync(c => c.Id == couponId);
        if (coupon == null)
            return new ErrorResult("Kupon bulunamadı.");

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
