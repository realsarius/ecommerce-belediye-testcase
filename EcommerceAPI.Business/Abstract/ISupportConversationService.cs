using EcommerceAPI.Core.Utilities.Results;
using EcommerceAPI.Entities.DTOs;

namespace EcommerceAPI.Business.Abstract;

public interface ISupportConversationService
{
    Task<IDataResult<SupportConversationDto>> GetOrCreateConversationAsync(int customerUserId, StartSupportConversationRequest request);

    Task<IDataResult<List<SupportConversationDto>>> GetMyConversationsAsync(int requesterUserId, string requesterRole);

    Task<IDataResult<PaginatedResponse<SupportConversationDto>>> GetQueueAsync(
        int requesterUserId,
        string requesterRole,
        int page = 1,
        int pageSize = 20);

    Task<IDataResult<PaginatedResponse<SupportMessageDto>>> GetMessagesAsync(
        int conversationId,
        int requesterUserId,
        string requesterRole,
        int page = 1,
        int pageSize = 50);

    Task<IDataResult<SupportMessageDto>> SendMessageAsync(
        int conversationId,
        int senderUserId,
        string senderRole,
        SendSupportMessageRequest request);

    Task<IDataResult<SupportConversationDto>> AssignConversationAsync(
        int conversationId,
        AssignSupportConversationRequest request,
        int requesterUserId,
        string requesterRole);

    Task<IDataResult<SupportConversationDto>> CloseConversationAsync(
        int conversationId,
        int requesterUserId,
        string requesterRole);

    Task<bool> CanAccessConversationAsync(int conversationId, int requesterUserId, string requesterRole);
}
