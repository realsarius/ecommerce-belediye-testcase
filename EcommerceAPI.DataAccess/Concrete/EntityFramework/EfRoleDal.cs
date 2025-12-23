using EcommerceAPI.Core.DataAccess.EntityFramework;
using EcommerceAPI.DataAccess.Abstract;
using EcommerceAPI.DataAccess.Concrete.EntityFramework.Contexts;
using EcommerceAPI.Entities.Concrete;

namespace EcommerceAPI.DataAccess.Concrete.EntityFramework;

public class EfRoleDal : EfEntityRepositoryBase<Role, AppDbContext>, IRoleDal
{
    public EfRoleDal(AppDbContext context) : base(context) { }
}
