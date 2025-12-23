using EcommerceAPI.Core.DataAccess;
using EcommerceAPI.Entities.Concrete;

namespace EcommerceAPI.DataAccess.Abstract;

public interface IShippingAddressDal : IEntityRepository<ShippingAddress>
{
}
