using EcommerceAPI.Core.DataAccess.EntityFramework;
using EcommerceAPI.Application.Abstractions.Persistence;
using EcommerceAPI.DataAccess.Concrete.EntityFramework.Contexts;
using EcommerceAPI.Entities.Concrete;

namespace EcommerceAPI.DataAccess.Concrete.EntityFramework;

public class EfReturnRequestAttachmentDal : EfEntityRepositoryBase<ReturnRequestAttachment, AppDbContext>, IReturnRequestAttachmentDal
{
    public EfReturnRequestAttachmentDal(AppDbContext context) : base(context)
    {
    }
}
