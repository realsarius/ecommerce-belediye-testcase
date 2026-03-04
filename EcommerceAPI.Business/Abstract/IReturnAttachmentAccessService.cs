using EcommerceAPI.Core.Utilities.Results;
using EcommerceAPI.Entities.Concrete;
using EcommerceAPI.Entities.DTOs;

namespace EcommerceAPI.Business.Abstract;

public interface IReturnAttachmentAccessService
{
    Task<IDataResult<ReturnAttachmentAccessUrlDto>> CreateSignedAccessUrlAsync(
        int requesterUserId,
        string? requesterRole,
        int returnRequestId,
        int attachmentId,
        string publicBaseUrl);

    Task<IDataResult<(ReturnRequestAttachment Attachment, string AbsolutePath)>> ValidateAccessTokenAsync(
        int attachmentId,
        string token);
}
