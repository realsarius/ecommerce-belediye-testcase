using EcommerceAPI.Core.DataAccess;
using EcommerceAPI.Entities.Concrete;

namespace EcommerceAPI.DataAccess.Abstract;

public interface IRefundRequestDal : IEntityRepository<RefundRequest>
{
    Task<RefundRequest?> GetByIdWithDetailsAsync(int id);
    Task<RefundRequest?> GetByReturnRequestIdAsync(int returnRequestId);
}
