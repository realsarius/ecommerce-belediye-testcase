using EcommerceAPI.Entities.DTOs;
using EcommerceAPI.Core.Utilities.Results;

namespace EcommerceAPI.Business.Abstract;

public interface ISellerProfileService
{
    Task<IDataResult<SellerProfileDto>> GetByUserIdAsync(int userId);
    Task<IDataResult<SellerProfileDto>> GetByIdAsync(int profileId);
    Task<IDataResult<SellerProfileDto>> CreateAsync(int userId, CreateSellerProfileRequest request);
    Task<IDataResult<SellerProfileDto>> UpdateAsync(int userId, UpdateSellerProfileRequest request);
    Task<IResult> DeleteAsync(int userId);
    Task<bool> HasProfileAsync(int userId);
}
