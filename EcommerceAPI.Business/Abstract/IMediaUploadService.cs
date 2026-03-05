using EcommerceAPI.Core.Utilities.Results;
using EcommerceAPI.Entities.DTOs;

namespace EcommerceAPI.Business.Abstract;

public interface IMediaUploadService
{
    Task<IDataResult<PresignedMediaUploadDto>> GetPresignedUploadUrlAsync(
        int userId,
        bool isAdmin,
        PresignMediaUploadRequest request);

    Task<IDataResult<ConfirmMediaUploadDto>> ConfirmUploadAsync(
        int userId,
        bool isAdmin,
        ConfirmMediaUploadRequest request);

    Task<IResult> DeleteProductImageAsync(
        int userId,
        bool isAdmin,
        int imageId);

    Task<IResult> ReorderProductImagesAsync(
        int userId,
        bool isAdmin,
        int productId,
        ReorderProductImagesRequest request);
}
