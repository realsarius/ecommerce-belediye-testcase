using System.Net;
using EcommerceAPI.Business.Abstract;
using EcommerceAPI.Core.Interfaces;
using EcommerceAPI.Core.Utilities.Results;
using EcommerceAPI.DataAccess.Abstract;
using EcommerceAPI.Entities.Concrete;
using EcommerceAPI.Entities.DTOs;
using EcommerceAPI.Entities.Enums;
using Microsoft.Extensions.Logging;

namespace EcommerceAPI.Business.Concrete;

public class AnnouncementManager : IAnnouncementService
{
    private static readonly HashSet<string> SupportedChannels = ["InApp", "Email"];

    private readonly IAnnouncementDal _announcementDal;
    private readonly IUserDal _userDal;
    private readonly INotificationService _notificationService;
    private readonly IEmailNotificationService _emailNotificationService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<AnnouncementManager> _logger;

    public AnnouncementManager(
        IAnnouncementDal announcementDal,
        IUserDal userDal,
        INotificationService notificationService,
        IEmailNotificationService emailNotificationService,
        IUnitOfWork unitOfWork,
        ILogger<AnnouncementManager> logger)
    {
        _announcementDal = announcementDal;
        _userDal = userDal;
        _notificationService = notificationService;
        _emailNotificationService = emailNotificationService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<IDataResult<AnnouncementDto>> CreateAsync(int adminUserId, CreateAnnouncementRequest request)
    {
        if (adminUserId <= 0)
        {
            return new ErrorDataResult<AnnouncementDto>("Geçersiz admin kullanıcısı.");
        }

        if (string.IsNullOrWhiteSpace(request.Title) || string.IsNullOrWhiteSpace(request.Message))
        {
            return new ErrorDataResult<AnnouncementDto>("Duyuru başlığı ve mesajı zorunludur.");
        }

        if (!TryParseAudienceType(request.AudienceType, out var audienceType))
        {
            return new ErrorDataResult<AnnouncementDto>("Geçersiz hedef kitle tipi.");
        }

        var channels = NormalizeChannels(request.Channels);
        if (channels.Count == 0)
        {
            return new ErrorDataResult<AnnouncementDto>("En az bir gönderim kanalı seçilmelidir.");
        }

        if (audienceType == AnnouncementAudienceType.Role && string.IsNullOrWhiteSpace(request.TargetRole))
        {
            return new ErrorDataResult<AnnouncementDto>("Rol bazlı duyuruda hedef rol zorunludur.");
        }

        if (audienceType == AnnouncementAudienceType.SpecificUsers && request.TargetUserIds.Count == 0)
        {
            return new ErrorDataResult<AnnouncementDto>("Belirli kullanıcılar hedefleniyorsa en az bir kullanıcı seçilmelidir.");
        }

        var targets = await ResolveTargetsAsync(audienceType, request.TargetRole, request.TargetUserIds);
        if (targets.Count == 0)
        {
            return new ErrorDataResult<AnnouncementDto>("Duyuru için uygun hedef kullanıcı bulunamadı.");
        }

        var now = DateTime.UtcNow;
        DateTime? scheduledAt = request.ScheduledAt.HasValue
            ? DateTime.SpecifyKind(request.ScheduledAt.Value, request.ScheduledAt.Value.Kind == DateTimeKind.Unspecified ? DateTimeKind.Utc : request.ScheduledAt.Value.Kind).ToUniversalTime()
            : null;
        var isScheduled = scheduledAt.HasValue && scheduledAt.Value > now.AddMinutes(1);

        var announcement = new Announcement
        {
            CreatedByUserId = adminUserId,
            Title = request.Title.Trim(),
            Message = request.Message.Trim(),
            AudienceType = audienceType,
            TargetRole = NormalizeOptional(request.TargetRole),
            TargetUserIds = audienceType == AnnouncementAudienceType.SpecificUsers
                ? string.Join(',', targets.Select(user => user.Id))
                : null,
            Channels = string.Join(',', channels),
            Status = isScheduled ? AnnouncementStatus.Scheduled : AnnouncementStatus.Processing,
            ScheduledAt = isScheduled ? scheduledAt : null,
            RecipientCount = targets.Count,
            CreatedAt = now,
            UpdatedAt = now
        };

        await _announcementDal.AddAsync(announcement);
        await _unitOfWork.SaveChangesAsync();

        var saved = await _announcementDal.GetByIdWithCreatorAsync(announcement.Id) ?? announcement;
        return new SuccessDataResult<AnnouncementDto>(MapToDto(saved));
    }

    public async Task<IDataResult<AnnouncementDto>> GetByIdAsync(int id)
    {
        var announcement = await _announcementDal.GetByIdWithCreatorAsync(id);
        if (announcement == null)
        {
            return new ErrorDataResult<AnnouncementDto>("Duyuru bulunamadı.");
        }

        return new SuccessDataResult<AnnouncementDto>(MapToDto(announcement));
    }

    public async Task<IDataResult<List<AnnouncementDto>>> GetRecentAsync(int take = 20)
    {
        var announcements = await _announcementDal.GetRecentWithCreatorAsync(take);
        return new SuccessDataResult<List<AnnouncementDto>>(announcements.Select(MapToDto).ToList());
    }

    public async Task SendAnnouncementAsync(int announcementId)
    {
        var announcement = await _announcementDal.GetByIdWithCreatorAsync(announcementId);
        if (announcement == null)
        {
            _logger.LogWarning("Announcement could not be dispatched because it was not found. AnnouncementId={AnnouncementId}", announcementId);
            return;
        }

        if (announcement.SentAt.HasValue && announcement.Status is AnnouncementStatus.Sent or AnnouncementStatus.PartiallySent)
        {
            return;
        }

        announcement.Status = AnnouncementStatus.Processing;
        announcement.UpdatedAt = DateTime.UtcNow;
        _announcementDal.Update(announcement);
        await _unitOfWork.SaveChangesAsync();

        var targetUserIds = ParseTargetUserIds(announcement.TargetUserIds);
        var targets = await ResolveTargetsAsync(announcement.AudienceType, announcement.TargetRole, targetUserIds);
        var channels = NormalizeChannels(announcement.Channels.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));

        if (targets.Count == 0 || channels.Count == 0)
        {
            announcement.Status = AnnouncementStatus.Failed;
            announcement.RecipientCount = targets.Count;
            announcement.DeliveredCount = 0;
            announcement.FailedCount = targets.Count;
            announcement.SentAt = DateTime.UtcNow;
            announcement.UpdatedAt = announcement.SentAt.Value;
            _announcementDal.Update(announcement);
            await _unitOfWork.SaveChangesAsync();
            return;
        }

        var deliveredCount = 0;
        var failedCount = 0;
        foreach (var user in targets)
        {
            var delivered = false;

            if (channels.Contains("InApp"))
            {
                var notificationResult = await _notificationService.CreateNotificationAsync(new CreateNotificationRequest
                {
                    UserId = user.Id,
                    Type = NotificationType.Announcement.ToString(),
                    Title = announcement.Title,
                    Body = announcement.Message,
                    DeepLink = "/notifications"
                });

                delivered |= notificationResult.Success;
            }

            if (channels.Contains("Email") && !string.IsNullOrWhiteSpace(user.Email))
            {
                var emailSent = await _emailNotificationService.SendAsync(
                    user.Email,
                    announcement.Title,
                    BuildEmailBody(announcement.Title, announcement.Message));

                delivered |= emailSent;
            }

            if (delivered)
            {
                deliveredCount += 1;
            }
            else
            {
                failedCount += 1;
            }
        }

        announcement.RecipientCount = targets.Count;
        announcement.DeliveredCount = deliveredCount;
        announcement.FailedCount = failedCount;
        announcement.SentAt = DateTime.UtcNow;
        announcement.UpdatedAt = announcement.SentAt.Value;
        announcement.Status = deliveredCount == 0
            ? AnnouncementStatus.Failed
            : failedCount == 0
                ? AnnouncementStatus.Sent
                : AnnouncementStatus.PartiallySent;

        _announcementDal.Update(announcement);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation(
            "Announcement dispatched. AnnouncementId={AnnouncementId}, Status={Status}, Delivered={DeliveredCount}, Failed={FailedCount}, Channels={Channels}",
            announcement.Id,
            announcement.Status,
            announcement.DeliveredCount,
            announcement.FailedCount,
            announcement.Channels);
    }

    private async Task<List<User>> ResolveTargetsAsync(
        AnnouncementAudienceType audienceType,
        string? targetRole,
        IEnumerable<int>? targetUserIds)
    {
        var users = (await _userDal.GetUsersWithRolesAsync())
            .Where(user => user.AccountStatus == UserAccountStatus.Active)
            .ToList();

        return audienceType switch
        {
            AnnouncementAudienceType.AllUsers => users,
            AnnouncementAudienceType.AllSellers => users
                .Where(user => string.Equals(user.Role?.Name, "Seller", StringComparison.OrdinalIgnoreCase))
                .ToList(),
            AnnouncementAudienceType.Role => users
                .Where(user => string.Equals(user.Role?.Name, targetRole?.Trim(), StringComparison.OrdinalIgnoreCase))
                .ToList(),
            AnnouncementAudienceType.SpecificUsers => users
                .Where(user => (targetUserIds ?? []).Contains(user.Id))
                .ToList(),
            _ => []
        };
    }

    private static AnnouncementDto MapToDto(Announcement announcement)
    {
        return new AnnouncementDto
        {
            Id = announcement.Id,
            Title = announcement.Title,
            Message = announcement.Message,
            AudienceType = announcement.AudienceType.ToString(),
            TargetRole = announcement.TargetRole,
            TargetUserIds = ParseTargetUserIds(announcement.TargetUserIds),
            Channels = NormalizeChannels(announcement.Channels.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)),
            Status = announcement.Status.ToString(),
            RecipientCount = announcement.RecipientCount,
            DeliveredCount = announcement.DeliveredCount,
            FailedCount = announcement.FailedCount,
            ScheduledAt = announcement.ScheduledAt,
            SentAt = announcement.SentAt,
            CreatedAt = announcement.CreatedAt,
            CreatedByName = BuildUserDisplayName(announcement.CreatedByUser)
        };
    }

    private static string BuildUserDisplayName(User? user)
    {
        if (user == null)
        {
            return "Sistem";
        }

        var fullName = $"{user.FirstName} {user.LastName}".Trim();
        return string.IsNullOrWhiteSpace(fullName) ? user.Email : fullName;
    }

    private static bool TryParseAudienceType(string? value, out AnnouncementAudienceType audienceType)
    {
        return Enum.TryParse(value?.Trim(), true, out audienceType);
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static List<string> NormalizeChannels(IEnumerable<string>? channels)
    {
        return (channels ?? [])
            .Where(channel => !string.IsNullOrWhiteSpace(channel))
            .Select(channel => channel.Trim())
            .Where(channel => SupportedChannels.Contains(channel))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(channel => channel.Equals("email", StringComparison.OrdinalIgnoreCase) ? "Email" : "InApp")
            .ToList();
    }

    private static List<int> ParseTargetUserIds(string? rawIds)
    {
        if (string.IsNullOrWhiteSpace(rawIds))
        {
            return [];
        }

        return rawIds
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(id => int.TryParse(id, out var parsed) ? parsed : 0)
            .Where(id => id > 0)
            .Distinct()
            .ToList();
    }

    private static string BuildEmailBody(string title, string message)
    {
        var safeTitle = WebUtility.HtmlEncode(title);
        var safeBody = WebUtility.HtmlEncode(message).Replace("\n", "<br />", StringComparison.Ordinal);
        return $"""
            <div style="font-family:Arial,sans-serif;line-height:1.6;color:#111827">
              <h2 style="margin-bottom:12px">{safeTitle}</h2>
              <p style="margin:0 0 16px">{safeBody}</p>
              <p style="margin:0;color:#6b7280;font-size:13px">Bu mesaj yönetim panelinden gönderilen bir duyurudur.</p>
            </div>
            """;
    }
}
