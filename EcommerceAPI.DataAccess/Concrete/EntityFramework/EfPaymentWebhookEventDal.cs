using EcommerceAPI.Core.DataAccess.EntityFramework;
using EcommerceAPI.DataAccess.Abstract;
using EcommerceAPI.DataAccess.Concrete.EntityFramework.Contexts;
using EcommerceAPI.Entities.Concrete;
using EcommerceAPI.Entities.Enums;
using Microsoft.EntityFrameworkCore;

namespace EcommerceAPI.DataAccess.Concrete.EntityFramework;

public class EfPaymentWebhookEventDal : EfEntityRepositoryBase<PaymentWebhookEvent, AppDbContext>, IPaymentWebhookEventDal
{
    public EfPaymentWebhookEventDal(AppDbContext context) : base(context)
    {
    }

    public Task<bool> ExistsByDedupeKeyAsync(PaymentProviderType provider, string dedupeKey)
    {
        return _dbSet.AnyAsync(x => x.Provider == provider && x.DedupeKey == dedupeKey);
    }
}
