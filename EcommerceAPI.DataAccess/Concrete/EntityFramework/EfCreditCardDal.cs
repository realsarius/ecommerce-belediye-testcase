using EcommerceAPI.Core.DataAccess.EntityFramework;
using EcommerceAPI.DataAccess.Abstract;
using EcommerceAPI.DataAccess.Concrete.EntityFramework.Contexts;
using EcommerceAPI.Entities.Concrete;

namespace EcommerceAPI.DataAccess.Concrete.EntityFramework;

public class EfCreditCardDal : EfEntityRepositoryBase<CreditCard, AppDbContext>, ICreditCardDal
{
    public EfCreditCardDal(AppDbContext context) : base(context)
    {
    }
}
