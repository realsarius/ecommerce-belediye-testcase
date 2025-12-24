using EcommerceAPI.Core.Utilities.Results;
using EcommerceAPI.Entities.DTOs;

namespace EcommerceAPI.Business.Abstract;

public interface ICouponService
{
    // Admin CRUD operations
    Task<IDataResult<List<CouponDto>>> GetAllAsync();
    Task<IDataResult<CouponDto>> GetByIdAsync(int id);
    Task<IDataResult<CouponDto>> CreateAsync(CreateCouponRequest request);
    Task<IDataResult<CouponDto>> UpdateAsync(int id, UpdateCouponRequest request);
    Task<IResult> DeleteAsync(int id);
    
    // User operations
    Task<IDataResult<CouponValidationResult>> ValidateCouponAsync(string code, decimal orderTotal);
    
    // Order integration (called within transaction)
    Task<IResult> IncrementUsageAsync(int couponId);
}
