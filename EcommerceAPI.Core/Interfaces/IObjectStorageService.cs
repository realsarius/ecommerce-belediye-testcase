namespace EcommerceAPI.Core.Interfaces;

public interface IObjectStorageService
{
    Task<PresignedUploadUrlResult> GeneratePresignedUploadUrlAsync(
        string objectKey,
        string contentType,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(string objectKey, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ObjectStorageObjectInfo>> ListObjectsAsync(
        string? prefix = null,
        int? maxKeys = null,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(string objectKey, CancellationToken cancellationToken = default);

    string GetPublicUrl(string objectKey);
}

public sealed record PresignedUploadUrlResult(
    string UploadUrl,
    string PublicUrl,
    string ObjectKey);

public sealed record ObjectStorageObjectInfo(
    string ObjectKey,
    DateTime LastModifiedUtc);
