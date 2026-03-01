using EcommerceAPI.Core.Utilities.Results;
using EcommerceAPI.Entities.DTOs;

namespace EcommerceAPI.Business.Abstract;

public interface IReturnRequestService
{
    Task<IDataResult<ReturnRequestDto>> CreateReturnRequestAsync(int userId, int orderId, CreateReturnRequestRequest request);
    Task<IDataResult<List<ReturnRequestDto>>> GetUserReturnRequestsAsync(int userId);
    Task<IDataResult<List<ReturnRequestDto>>> GetPendingReturnRequestsAsync(int? sellerId = null);
    Task<IDataResult<ReturnRequestDto>> ReviewReturnRequestAsync(int requestId, int reviewerUserId, ReviewReturnRequestRequest request, int? sellerId = null);
}
