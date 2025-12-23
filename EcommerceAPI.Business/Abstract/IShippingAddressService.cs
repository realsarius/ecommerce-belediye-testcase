using EcommerceAPI.Entities.DTOs;
using EcommerceAPI.Core.Utilities.Results;

namespace EcommerceAPI.Business.Abstract;

public interface IShippingAddressService
{
    Task<IDataResult<List<ShippingAddressDto>>> GetUserAddressesAsync(int userId);
    Task<IDataResult<ShippingAddressDto>> AddAddressAsync(int userId, CreateShippingAddressRequest request);
}
