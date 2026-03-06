using EcommerceAPI.Core.Utilities.Results;
using EcommerceAPI.Entities.DTOs;

namespace EcommerceAPI.Business.Abstract;

public interface IAnnouncementService
{
    Task<IDataResult<AnnouncementDto>> CreateAsync(int adminUserId, CreateAnnouncementRequest request);
    Task<IDataResult<AnnouncementDto>> GetByIdAsync(int id);
    Task<IDataResult<List<AnnouncementDto>>> GetRecentAsync(int take = 20);
    Task SendAnnouncementAsync(int announcementId);
}
