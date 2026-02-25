using EcommerceAPI.Business.Abstract;
using EcommerceAPI.Business.Constants;
using EcommerceAPI.Core.CrossCuttingConcerns.Logging;
using EcommerceAPI.Core.Interfaces;
using EcommerceAPI.Core.Utilities.Results;
using EcommerceAPI.DataAccess.Abstract;
using EcommerceAPI.Entities.Concrete;
using EcommerceAPI.Entities.DTOs;
using EcommerceAPI.Entities.Enums;

namespace EcommerceAPI.Business.Concrete;

public class SupportConversationManager : ISupportConversationService
{
    private readonly ISupportConversationDal _supportConversationDal;
    private readonly ISupportMessageDal _supportMessageDal;
    private readonly IUserDal _userDal;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditService _auditService;

    public SupportConversationManager(
        ISupportConversationDal supportConversationDal,
        ISupportMessageDal supportMessageDal,
        IUserDal userDal,
        IUnitOfWork unitOfWork,
        IAuditService auditService)
    {
        _supportConversationDal = supportConversationDal;
        _supportMessageDal = supportMessageDal;
        _userDal = userDal;
        _unitOfWork = unitOfWork;
        _auditService = auditService;
    }

    public async Task<IDataResult<SupportConversationDto>> GetOrCreateConversationAsync(int customerUserId, StartSupportConversationRequest request)
    {
        var subject = string.IsNullOrWhiteSpace(request.Subject) ? "Canlı Destek" : request.Subject.Trim();

        var existing = (await _supportConversationDal.GetByCustomerUserIdAsync(customerUserId, onlyOpen: true))
            .OrderByDescending(x => x.LastMessageAt ?? x.CreatedAt)
            .FirstOrDefault();

        if (existing != null)
        {
            if (!string.IsNullOrWhiteSpace(request.InitialMessage))
            {
                await SendMessageAsync(
                    existing.Id,
                    customerUserId,
                    "Customer",
                    new SendSupportMessageRequest { Message = request.InitialMessage });
            }

            var refreshed = await _supportConversationDal.GetByIdWithDetailsAsync(existing.Id) ?? existing;
            return new SuccessDataResult<SupportConversationDto>(
                MapConversationDto(refreshed),
                Messages.SupportConversationAlreadyOpen);
        }

        var conversation = new SupportConversation
        {
            Subject = subject,
            CustomerUserId = customerUserId,
            Status = SupportConversationStatus.Open,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _supportConversationDal.AddAsync(conversation);

        if (!string.IsNullOrWhiteSpace(request.InitialMessage))
        {
            var now = DateTime.UtcNow;
            conversation.LastMessageAt = now;

            await _supportMessageDal.AddAsync(new SupportMessage
            {
                Conversation = conversation,
                SenderUserId = customerUserId,
                SenderRole = "Customer",
                Message = request.InitialMessage.Trim(),
                IsSystemMessage = false,
                CreatedAt = now,
                UpdatedAt = now
            });
        }

        await _unitOfWork.SaveChangesAsync();

        await _auditService.LogActionAsync(
            customerUserId.ToString(),
            "StartSupportConversation",
            "SupportConversation",
            new { ConversationId = conversation.Id });

        var created = await _supportConversationDal.GetByIdWithDetailsAsync(conversation.Id) ?? conversation;
        return new SuccessDataResult<SupportConversationDto>(
            MapConversationDto(created),
            Messages.SupportConversationCreated);
    }

    public async Task<IDataResult<List<SupportConversationDto>>> GetMyConversationsAsync(int requesterUserId, string requesterRole)
    {
        var role = NormalizeRole(requesterRole);
        List<SupportConversation> conversations;

        if (role == "Customer")
        {
            conversations = await _supportConversationDal.GetByCustomerUserIdAsync(requesterUserId);
        }
        else if (role == "Support")
        {
            conversations = await _supportConversationDal.GetAssignedToSupportAsync(requesterUserId, 1, 100);
        }
        else if (role == "Admin")
        {
            conversations = await _supportConversationDal.GetQueueAsync(1, 100);
        }
        else
        {
            return new ErrorDataResult<List<SupportConversationDto>>(Messages.SupportUnauthorizedAction);
        }

        var dto = conversations.Select(MapConversationDto).ToList();
        return new SuccessDataResult<List<SupportConversationDto>>(dto);
    }

    public async Task<IDataResult<PaginatedResponse<SupportConversationDto>>> GetQueueAsync(
        int requesterUserId,
        string requesterRole,
        int page = 1,
        int pageSize = 20)
    {
        var role = NormalizeRole(requesterRole);
        if (!IsSupportOrAdmin(role))
        {
            return new ErrorDataResult<PaginatedResponse<SupportConversationDto>>(Messages.SupportUnauthorizedAction);
        }

        page = page < 1 ? 1 : page;
        pageSize = pageSize < 1 ? 20 : Math.Min(pageSize, 100);

        var items = await _supportConversationDal.GetQueueAsync(page, pageSize);
        var totalCount = await _supportConversationDal.CountAsync(x => x.Status == SupportConversationStatus.Open);

        return new SuccessDataResult<PaginatedResponse<SupportConversationDto>>(
            new PaginatedResponse<SupportConversationDto>
            {
                Items = items.Select(MapConversationDto).ToList(),
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount
            });
    }

    public async Task<IDataResult<PaginatedResponse<SupportMessageDto>>> GetMessagesAsync(
        int conversationId,
        int requesterUserId,
        string requesterRole,
        int page = 1,
        int pageSize = 50)
    {
        var hasAccess = await CanAccessConversationAsync(conversationId, requesterUserId, requesterRole);
        if (!hasAccess)
        {
            return new ErrorDataResult<PaginatedResponse<SupportMessageDto>>(Messages.SupportConversationAccessDenied);
        }

        page = page < 1 ? 1 : page;
        pageSize = pageSize < 1 ? 50 : Math.Min(pageSize, 200);

        var items = await _supportMessageDal.GetConversationMessagesAsync(conversationId, page, pageSize);
        var totalCount = await _supportMessageDal.CountAsync(x => x.ConversationId == conversationId);

        return new SuccessDataResult<PaginatedResponse<SupportMessageDto>>(
            new PaginatedResponse<SupportMessageDto>
            {
                Items = items.Select(x => MapMessageDto(x)).ToList(),
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount
            });
    }

    public async Task<IDataResult<SupportMessageDto>> SendMessageAsync(
        int conversationId,
        int senderUserId,
        string senderRole,
        SendSupportMessageRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Message))
        {
            return new ErrorDataResult<SupportMessageDto>(Messages.SupportMessageEmpty);
        }

        var role = NormalizeRole(senderRole);
        if (role != "Customer" && role != "Support" && role != "Admin")
        {
            return new ErrorDataResult<SupportMessageDto>(Messages.SupportUnauthorizedAction);
        }

        var hasAccess = await CanAccessConversationAsync(conversationId, senderUserId, role);
        if (!hasAccess)
        {
            return new ErrorDataResult<SupportMessageDto>(Messages.SupportConversationAccessDenied);
        }

        var conversation = await _supportConversationDal.GetAsync(x => x.Id == conversationId);
        if (conversation == null)
        {
            return new ErrorDataResult<SupportMessageDto>(Messages.SupportConversationNotFound);
        }

        if (conversation.Status == SupportConversationStatus.Closed)
        {
            return new ErrorDataResult<SupportMessageDto>(Messages.SupportConversationAlreadyClosed);
        }

        var now = DateTime.UtcNow;
        var messageEntity = new SupportMessage
        {
            ConversationId = conversationId,
            SenderUserId = senderUserId,
            SenderRole = role,
            Message = request.Message.Trim(),
            IsSystemMessage = false,
            CreatedAt = now,
            UpdatedAt = now
        };

        await _supportMessageDal.AddAsync(messageEntity);

        conversation.LastMessageAt = now;
        conversation.UpdatedAt = now;

        if (role == "Support" && conversation.SupportUserId == null)
        {
            conversation.SupportUserId = senderUserId;
            conversation.Status = SupportConversationStatus.Assigned;
        }

        _supportConversationDal.Update(conversation);
        await _unitOfWork.SaveChangesAsync();

        await _auditService.LogActionAsync(
            senderUserId.ToString(),
            "SendSupportMessage",
            "SupportConversation",
            new { ConversationId = conversationId, SenderRole = role });

        var sender = await _userDal.GetAsync(x => x.Id == senderUserId);

        return new SuccessDataResult<SupportMessageDto>(
            MapMessageDto(messageEntity, sender),
            Messages.SupportMessageSent);
    }

    public async Task<IDataResult<SupportConversationDto>> AssignConversationAsync(
        int conversationId,
        AssignSupportConversationRequest request,
        int requesterUserId,
        string requesterRole)
    {
        var role = NormalizeRole(requesterRole);
        if (!IsSupportOrAdmin(role))
        {
            return new ErrorDataResult<SupportConversationDto>(Messages.SupportUnauthorizedAction);
        }

        if (request.SupportUserId <= 0)
        {
            return new ErrorDataResult<SupportConversationDto>(Messages.SupportInvalidSupportUser);
        }

        if (role == "Support" && requesterUserId != request.SupportUserId)
        {
            return new ErrorDataResult<SupportConversationDto>(Messages.AuthorizationDenied);
        }

        var conversation = await _supportConversationDal.GetAsync(x => x.Id == conversationId);
        if (conversation == null)
        {
            return new ErrorDataResult<SupportConversationDto>(Messages.SupportConversationNotFound);
        }

        if (conversation.Status == SupportConversationStatus.Closed)
        {
            return new ErrorDataResult<SupportConversationDto>(Messages.SupportConversationAlreadyClosed);
        }

        var now = DateTime.UtcNow;

        conversation.SupportUserId = request.SupportUserId;
        conversation.Status = SupportConversationStatus.Assigned;
        conversation.UpdatedAt = now;
        conversation.LastMessageAt = now;

        _supportConversationDal.Update(conversation);

        await _supportMessageDal.AddAsync(new SupportMessage
        {
            ConversationId = conversationId,
            SenderUserId = requesterUserId,
            SenderRole = role,
            Message = $"Görüşme destek temsilcisine atandı (UserId: {request.SupportUserId}).",
            IsSystemMessage = true,
            CreatedAt = now,
            UpdatedAt = now
        });

        await _unitOfWork.SaveChangesAsync();

        await _auditService.LogActionAsync(
            requesterUserId.ToString(),
            "AssignSupportConversation",
            "SupportConversation",
            new { ConversationId = conversationId, SupportUserId = request.SupportUserId });

        var updated = await _supportConversationDal.GetByIdWithDetailsAsync(conversationId);
        if (updated == null)
        {
            return new ErrorDataResult<SupportConversationDto>(Messages.SupportConversationNotFound);
        }

        return new SuccessDataResult<SupportConversationDto>(
            MapConversationDto(updated),
            Messages.SupportConversationAssigned);
    }

    public async Task<IDataResult<SupportConversationDto>> CloseConversationAsync(
        int conversationId,
        int requesterUserId,
        string requesterRole)
    {
        var hasAccess = await CanAccessConversationAsync(conversationId, requesterUserId, requesterRole);
        if (!hasAccess)
        {
            return new ErrorDataResult<SupportConversationDto>(Messages.SupportConversationAccessDenied);
        }

        var conversation = await _supportConversationDal.GetAsync(x => x.Id == conversationId);
        if (conversation == null)
        {
            return new ErrorDataResult<SupportConversationDto>(Messages.SupportConversationNotFound);
        }

        if (conversation.Status == SupportConversationStatus.Closed)
        {
            return new ErrorDataResult<SupportConversationDto>(Messages.SupportConversationAlreadyClosed);
        }

        var role = NormalizeRole(requesterRole);
        var now = DateTime.UtcNow;

        conversation.Status = SupportConversationStatus.Closed;
        conversation.ClosedAt = now;
        conversation.LastMessageAt = now;
        conversation.UpdatedAt = now;

        _supportConversationDal.Update(conversation);

        await _supportMessageDal.AddAsync(new SupportMessage
        {
            ConversationId = conversationId,
            SenderUserId = requesterUserId,
            SenderRole = role,
            Message = "Görüşme kapatıldı.",
            IsSystemMessage = true,
            CreatedAt = now,
            UpdatedAt = now
        });

        await _unitOfWork.SaveChangesAsync();

        await _auditService.LogActionAsync(
            requesterUserId.ToString(),
            "CloseSupportConversation",
            "SupportConversation",
            new { ConversationId = conversationId });

        var updated = await _supportConversationDal.GetByIdWithDetailsAsync(conversationId);
        if (updated == null)
        {
            return new ErrorDataResult<SupportConversationDto>(Messages.SupportConversationNotFound);
        }

        return new SuccessDataResult<SupportConversationDto>(
            MapConversationDto(updated),
            Messages.SupportConversationClosed);
    }

    public async Task<bool> CanAccessConversationAsync(int conversationId, int requesterUserId, string requesterRole)
    {
        var conversation = await _supportConversationDal.GetAsync(x => x.Id == conversationId);
        if (conversation == null)
        {
            return false;
        }

        var role = NormalizeRole(requesterRole);

        if (role == "Admin")
        {
            return true;
        }

        if (role == "Customer")
        {
            return conversation.CustomerUserId == requesterUserId;
        }

        if (role == "Support")
        {
            if (conversation.SupportUserId.HasValue && conversation.SupportUserId.Value != requesterUserId)
            {
                return false;
            }

            return true;
        }

        return false;
    }

    private static SupportConversationDto MapConversationDto(SupportConversation conversation)
    {
        var lastMessage = conversation.Messages?
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefault();

        return new SupportConversationDto
        {
            Id = conversation.Id,
            Subject = conversation.Subject,
            CustomerUserId = conversation.CustomerUserId,
            CustomerName = BuildDisplayName(conversation.CustomerUser),
            SupportUserId = conversation.SupportUserId,
            SupportName = BuildDisplayName(conversation.SupportUser),
            Status = conversation.Status.ToString(),
            LastMessage = lastMessage?.Message,
            LastSenderRole = lastMessage?.SenderRole,
            LastMessageAt = conversation.LastMessageAt,
            ClosedAt = conversation.ClosedAt,
            CreatedAt = conversation.CreatedAt
        };
    }

    private static SupportMessageDto MapMessageDto(SupportMessage message, User? sender = null)
    {
        var senderName = sender != null
            ? BuildDisplayName(sender)
            : BuildDisplayName(message.SenderUser);

        return new SupportMessageDto
        {
            Id = message.Id,
            ConversationId = message.ConversationId,
            SenderUserId = message.SenderUserId,
            SenderRole = message.SenderRole,
            SenderName = senderName,
            Message = message.Message,
            IsSystemMessage = message.IsSystemMessage,
            CreatedAt = message.CreatedAt
        };
    }

    private static string BuildDisplayName(User? user)
    {
        if (user == null) return string.Empty;

        var fullName = $"{user.FirstName} {user.LastName}".Trim();
        return string.IsNullOrWhiteSpace(fullName) ? user.Email : fullName;
    }

    private static string NormalizeRole(string? role)
    {
        if (string.IsNullOrWhiteSpace(role)) return string.Empty;

        return role.Trim().ToLowerInvariant() switch
        {
            "admin" => "Admin",
            "support" => "Support",
            "customer" => "Customer",
            _ => role.Trim()
        };
    }

    private static bool IsSupportOrAdmin(string role)
    {
        return role == "Support" || role == "Admin";
    }
}
