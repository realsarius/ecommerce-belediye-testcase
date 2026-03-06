using EcommerceAPI.Core.Utilities.Results;

namespace EcommerceAPI.Business.Abstract;

public interface IPlatformSellerService
{
    Task<IDataResult<int>> GetOrCreatePlatformSellerIdAsync();
}
