using EcommerceAPI.Core.Utilities.Results;
using EcommerceAPI.Entities.Concrete;
using EcommerceAPI.Entities.DTOs;

namespace EcommerceAPI.Application.Abstractions.ServiceContracts;

public interface IReturnAttachmentStorageService
{
    Task<IDataResult<List<UploadedReturnPhotoDto>>> UploadTemporaryPhotosAsync(
        int userId,
        IEnumerable<ReturnAttachmentUploadContent> files,
        CancellationToken cancellationToken = default);

    Task<IDataResult<List<ReturnRequestAttachment>>> FinalizeTemporaryPhotosAsync(
        int userId,
        IEnumerable<string>? uploadKeys,
        CancellationToken cancellationToken = default);
}
