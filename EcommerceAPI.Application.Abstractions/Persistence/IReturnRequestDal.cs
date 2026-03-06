using EcommerceAPI.Core.DataAccess;
using EcommerceAPI.Entities.Concrete;
using EcommerceAPI.Entities.Enums;

namespace EcommerceAPI.Application.Abstractions.Persistence;

public interface IReturnRequestDal : IEntityRepository<ReturnRequest>
{
    Task<ReturnRequest?> GetByIdWithDetailsAsync(int id);
    Task<IList<ReturnRequest>> GetUserRequestsAsync(int userId);
    Task<IList<ReturnRequest>> GetListWithDetailsAsync(ReturnRequestStatus? status = null, int? sellerId = null);
    Task<IList<ReturnRequest>> GetBySellerIdAsync(int sellerId);
    Task<bool> HasActiveRequestForOrderAsync(int orderId);
}
