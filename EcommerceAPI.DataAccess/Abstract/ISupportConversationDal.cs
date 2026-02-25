using EcommerceAPI.Core.DataAccess;
using EcommerceAPI.Entities.Concrete;

namespace EcommerceAPI.DataAccess.Abstract;

public interface ISupportConversationDal : IEntityRepository<SupportConversation>
{
    Task<SupportConversation?> GetByIdWithDetailsAsync(int conversationId);
    Task<List<SupportConversation>> GetByCustomerUserIdAsync(int customerUserId, bool onlyOpen = false);
    Task<List<SupportConversation>> GetQueueAsync(int page, int pageSize);
    Task<List<SupportConversation>> GetQueueForSupportAsync(int supportUserId, int page, int pageSize);
    Task<List<SupportConversation>> GetAssignedToSupportAsync(int supportUserId, int page, int pageSize);
}
