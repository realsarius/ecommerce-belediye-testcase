using EcommerceAPI.Core.DataAccess;
using EcommerceAPI.Entities.Concrete;

namespace EcommerceAPI.DataAccess.Abstract;

public interface IReturnRequestDal : IEntityRepository<ReturnRequest>
{
    Task<ReturnRequest?> GetByIdWithDetailsAsync(int id);
    Task<IList<ReturnRequest>> GetUserRequestsAsync(int userId);
    Task<IList<ReturnRequest>> GetPendingRequestsAsync(int? sellerId = null);
    Task<bool> HasActiveRequestForOrderAsync(int orderId);
}
