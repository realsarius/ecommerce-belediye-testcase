using EcommerceAPI.Core.Utilities.Results;
using EcommerceAPI.Entities.DTOs;

namespace EcommerceAPI.Application.Abstractions.ServiceContracts;

public interface IAdminSellerService
{
    Task<IDataResult<List<AdminSellerListItemDto>>> GetSellersAsync(string? status = null);
    Task<IDataResult<AdminSellerDetailDto>> GetSellerDetailAsync(int sellerId);
    Task<IDataResult<AdminSellerDetailDto>> UpdateSellerStatusAsync(int sellerId, UpdateAdminSellerStatusRequest request);
    Task<IDataResult<AdminSellerDetailDto>> UpdateSellerCommissionAsync(int sellerId, UpdateAdminSellerCommissionRequest request);
    Task<IDataResult<AdminSellerDetailDto>> ApproveApplicationAsync(int sellerId, ReviewSellerApplicationRequest request);
    Task<IDataResult<AdminSellerDetailDto>> RejectApplicationAsync(int sellerId, ReviewSellerApplicationRequest request);
}
