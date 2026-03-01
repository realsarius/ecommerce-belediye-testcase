using EcommerceAPI.Core.DataAccess.EntityFramework;
using EcommerceAPI.DataAccess.Abstract;
using EcommerceAPI.DataAccess.Concrete.EntityFramework.Contexts;
using EcommerceAPI.Entities.Concrete;

namespace EcommerceAPI.DataAccess.Concrete.EntityFramework;

public class EfCampaignProductDal : EfEntityRepositoryBase<CampaignProduct, AppDbContext>, ICampaignProductDal
{
    public EfCampaignProductDal(AppDbContext context) : base(context)
    {
    }
}
