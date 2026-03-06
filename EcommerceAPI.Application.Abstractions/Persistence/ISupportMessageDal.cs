using EcommerceAPI.Core.DataAccess;
using EcommerceAPI.Entities.Concrete;

namespace EcommerceAPI.Application.Abstractions.Persistence;

public interface ISupportMessageDal : IEntityRepository<SupportMessage>
{
    Task<List<SupportMessage>> GetConversationMessagesAsync(int conversationId, int page, int pageSize);
}
