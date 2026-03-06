using EcommerceAPI.Core.Utilities.Results;

namespace EcommerceAPI.Application.Abstractions.ServiceContracts;

public interface IPlatformSellerService
{
    Task<IDataResult<int>> GetOrCreatePlatformSellerIdAsync();
}
