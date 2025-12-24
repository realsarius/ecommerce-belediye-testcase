using EcommerceAPI.Core.DataAccess.EntityFramework;
using EcommerceAPI.DataAccess.Abstract;
using EcommerceAPI.DataAccess.Concrete.EntityFramework.Contexts;
using EcommerceAPI.Entities.Concrete;

namespace EcommerceAPI.DataAccess.Concrete.EntityFramework;

public class EfShippingAddressDal : EfEntityRepositoryBase<ShippingAddress, AppDbContext>, IShippingAddressDal
{
    public EfShippingAddressDal(AppDbContext context) : base(context)
    {
    }
}
