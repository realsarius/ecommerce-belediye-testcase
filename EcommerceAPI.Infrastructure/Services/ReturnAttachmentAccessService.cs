using System.Text.Json;
using EcommerceAPI.Business.Abstract;
using EcommerceAPI.Core.Interfaces;
using EcommerceAPI.Core.Utilities.Results;
using EcommerceAPI.DataAccess.Abstract;
using EcommerceAPI.Entities.Concrete;
using EcommerceAPI.Entities.DTOs;
using EcommerceAPI.Infrastructure.Settings;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace EcommerceAPI.Infrastructure.Services;

public class ReturnAttachmentAccessService : IReturnAttachmentAccessService
{
    private readonly IReturnRequestDal _returnRequestDal;
    private readonly IReturnRequestAttachmentDal _returnRequestAttachmentDal;
    private readonly ISellerProfileService _sellerProfileService;
    private readonly IEncryptionService _encryptionService;
    private readonly ReturnAttachmentSettings _settings;
    private readonly string _storageRootPath;

    public ReturnAttachmentAccessService(
        IReturnRequestDal returnRequestDal,
        IReturnRequestAttachmentDal returnRequestAttachmentDal,
        ISellerProfileService sellerProfileService,
        IEncryptionService encryptionService,
        IHostEnvironment hostEnvironment,
        IOptions<ReturnAttachmentSettings> settings)
    {
        _returnRequestDal = returnRequestDal;
        _returnRequestAttachmentDal = returnRequestAttachmentDal;
        _sellerProfileService = sellerProfileService;
        _encryptionService = encryptionService;
        _settings = settings.Value;
        _storageRootPath = Path.IsPathRooted(_settings.RootPath)
            ? _settings.RootPath
            : Path.Combine(hostEnvironment.ContentRootPath, _settings.RootPath);
    }

    public async Task<IDataResult<ReturnAttachmentAccessUrlDto>> CreateSignedAccessUrlAsync(
        int requesterUserId,
        string? requesterRole,
        int returnRequestId,
        int attachmentId,
        string publicBaseUrl)
    {
        var returnRequest = await _returnRequestDal.GetByIdWithDetailsAsync(returnRequestId);
        if (returnRequest == null)
        {
            return new ErrorDataResult<ReturnAttachmentAccessUrlDto>("İade talebi bulunamadı.");
        }

        if (!await CanAccessAsync(requesterUserId, requesterRole, returnRequest))
        {
            return new ErrorDataResult<ReturnAttachmentAccessUrlDto>("Bu iade görsellerine erişim yetkiniz yok.");
        }

        var attachment = returnRequest.Attachments.FirstOrDefault(x => x.Id == attachmentId);
        if (attachment == null)
        {
            return new ErrorDataResult<ReturnAttachmentAccessUrlDto>("İade görseli bulunamadı.");
        }

        var expiresAt = DateTime.UtcNow.AddMinutes(_settings.SignedUrlLifetimeMinutes);
        var payload = new SignedAttachmentPayload
        {
            AttachmentId = attachmentId,
            ReturnRequestId = returnRequestId,
            ExpiresAt = expiresAt
        };

        var token = Uri.EscapeDataString(_encryptionService.Encrypt(JsonSerializer.Serialize(payload)));
        var normalizedBaseUrl = publicBaseUrl.TrimEnd('/');
        var url = $"{normalizedBaseUrl}/api/v1/returns/attachments/{attachmentId}/content?token={token}";

        return new SuccessDataResult<ReturnAttachmentAccessUrlDto>(new ReturnAttachmentAccessUrlDto
        {
            Url = url,
            ExpiresAt = expiresAt
        });
    }

    public async Task<IDataResult<(ReturnRequestAttachment Attachment, string AbsolutePath)>> ValidateAccessTokenAsync(
        int attachmentId,
        string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return new ErrorDataResult<(ReturnRequestAttachment Attachment, string AbsolutePath)>("Erişim anahtarı bulunamadı.");
        }

        SignedAttachmentPayload? payload;
        try
        {
            payload = JsonSerializer.Deserialize<SignedAttachmentPayload>(_encryptionService.Decrypt(token));
        }
        catch (Exception)
        {
            return new ErrorDataResult<(ReturnRequestAttachment Attachment, string AbsolutePath)>("Erişim anahtarı doğrulanamadı.");
        }

        if (payload == null || payload.AttachmentId != attachmentId || payload.ExpiresAt <= DateTime.UtcNow)
        {
            return new ErrorDataResult<(ReturnRequestAttachment Attachment, string AbsolutePath)>("Erişim bağlantısının süresi doldu veya geçersiz.");
        }

        var attachment = await _returnRequestAttachmentDal.GetAsync(x =>
            x.Id == attachmentId &&
            x.ReturnRequestId == payload.ReturnRequestId);

        if (attachment == null)
        {
            return new ErrorDataResult<(ReturnRequestAttachment Attachment, string AbsolutePath)>("İade görseli bulunamadı.");
        }

        var absolutePath = Path.Combine(_storageRootPath, attachment.RelativePath.Replace('/', Path.DirectorySeparatorChar));
        if (!File.Exists(absolutePath))
        {
            return new ErrorDataResult<(ReturnRequestAttachment Attachment, string AbsolutePath)>("İade görseli dosya sisteminde bulunamadı.");
        }

        return new SuccessDataResult<(ReturnRequestAttachment Attachment, string AbsolutePath)>((attachment, absolutePath));
    }

    private async Task<bool> CanAccessAsync(int requesterUserId, string? requesterRole, ReturnRequest returnRequest)
    {
        if (string.Equals(requesterRole, "Admin", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (string.Equals(requesterRole, "Seller", StringComparison.OrdinalIgnoreCase))
        {
            var sellerProfileResult = await _sellerProfileService.GetByUserIdAsync(requesterUserId);
            var sellerProfileId = sellerProfileResult.Success ? sellerProfileResult.Data?.Id : null;
            if (sellerProfileId.HasValue)
            {
                return returnRequest.Order.OrderItems.Any(item => item.Product.SellerId == sellerProfileId.Value);
            }
        }

        return returnRequest.UserId == requesterUserId;
    }

    private sealed class SignedAttachmentPayload
    {
        public int AttachmentId { get; set; }
        public int ReturnRequestId { get; set; }
        public DateTime ExpiresAt { get; set; }
    }
}
