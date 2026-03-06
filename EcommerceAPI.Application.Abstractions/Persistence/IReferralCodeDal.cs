using EcommerceAPI.Core.DataAccess;
using EcommerceAPI.Entities.Concrete;

namespace EcommerceAPI.Application.Abstractions.Persistence;

public interface IReferralCodeDal : IEntityRepository<ReferralCode>
{
    Task<ReferralCode?> GetByCodeAsync(string code);
    Task<ReferralCode?> GetByUserIdAsync(int userId);
}
