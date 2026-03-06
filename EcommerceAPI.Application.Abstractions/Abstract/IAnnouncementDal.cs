using EcommerceAPI.Core.DataAccess;
using EcommerceAPI.Entities.Concrete;

namespace EcommerceAPI.DataAccess.Abstract;

public interface IAnnouncementDal : IEntityRepository<Announcement>
{
    Task<Announcement?> GetByIdWithCreatorAsync(int id);
    Task<List<Announcement>> GetRecentWithCreatorAsync(int take = 20);
}
