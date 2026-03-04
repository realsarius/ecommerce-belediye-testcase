using EcommerceAPI.Business.Abstract;
using EcommerceAPI.Core.Utilities.Results;
using EcommerceAPI.Entities.Concrete;
using EcommerceAPI.Entities.DTOs;
using EcommerceAPI.Infrastructure.Settings;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace EcommerceAPI.Infrastructure.Services;

public class ReturnAttachmentStorageService : IReturnAttachmentStorageService
{
    private const string TempMetadataExtension = ".meta.json";
    private readonly ReturnAttachmentSettings _settings;
    private readonly string _storageRootPath;

    public ReturnAttachmentStorageService(
        IHostEnvironment hostEnvironment,
        IOptions<ReturnAttachmentSettings> settings)
    {
        _settings = settings.Value;
        _storageRootPath = Path.IsPathRooted(_settings.RootPath)
            ? _settings.RootPath
            : Path.Combine(hostEnvironment.ContentRootPath, _settings.RootPath);
    }

    public async Task<IDataResult<List<UploadedReturnPhotoDto>>> UploadTemporaryPhotosAsync(
        int userId,
        IEnumerable<ReturnAttachmentUploadContent> files,
        CancellationToken cancellationToken = default)
    {
        var uploads = files.ToList();
        if (uploads.Count == 0)
        {
            return new ErrorDataResult<List<UploadedReturnPhotoDto>>("Yüklenecek dosya bulunamadı.");
        }

        if (uploads.Count > _settings.MaxFiles)
        {
            return new ErrorDataResult<List<UploadedReturnPhotoDto>>($"En fazla {_settings.MaxFiles} fotoğraf yükleyebilirsiniz.");
        }

        Directory.CreateDirectory(GetTempDirectory(userId));

        var uploadedPhotos = new List<UploadedReturnPhotoDto>();

        foreach (var upload in uploads)
        {
            var validationError = ValidateUpload(upload);
            if (validationError != null)
            {
                return new ErrorDataResult<List<UploadedReturnPhotoDto>>(validationError);
            }

            var uploadKey = Guid.NewGuid().ToString("N");
            var storedFileName = $"{uploadKey}{Path.GetExtension(upload.FileName)}";
            var tempFilePath = Path.Combine(GetTempDirectory(userId), storedFileName);
            var metadataPath = GetMetadataPath(tempFilePath);

            await using (var fileStream = new FileStream(tempFilePath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                await upload.Content.CopyToAsync(fileStream, cancellationToken);
            }

            var metadata = new TemporaryReturnAttachmentMetadata
            {
                UploadKey = uploadKey,
                OriginalFileName = Path.GetFileName(upload.FileName),
                StoredFileName = storedFileName,
                ContentType = upload.ContentType.Trim(),
                SizeBytes = upload.SizeBytes
            };

            await File.WriteAllTextAsync(metadataPath, JsonSerializer.Serialize(metadata), cancellationToken);

            uploadedPhotos.Add(new UploadedReturnPhotoDto
            {
                UploadKey = uploadKey,
                FileName = metadata.OriginalFileName,
                ContentType = metadata.ContentType,
                SizeBytes = metadata.SizeBytes
            });
        }

        return new SuccessDataResult<List<UploadedReturnPhotoDto>>(uploadedPhotos);
    }

    public async Task<IDataResult<List<ReturnRequestAttachment>>> FinalizeTemporaryPhotosAsync(
        int userId,
        IEnumerable<string>? uploadKeys,
        CancellationToken cancellationToken = default)
    {
        var normalizedKeys = uploadKeys?
            .Where(key => !string.IsNullOrWhiteSpace(key))
            .Select(key => key.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList() ?? [];

        if (normalizedKeys.Count == 0)
        {
            return new SuccessDataResult<List<ReturnRequestAttachment>>([]);
        }

        if (normalizedKeys.Count > _settings.MaxFiles)
        {
            return new ErrorDataResult<List<ReturnRequestAttachment>>($"En fazla {_settings.MaxFiles} fotoğraf ekleyebilirsiniz.");
        }

        var finalizedAttachments = new List<ReturnRequestAttachment>();
        var finalizedFilePaths = new List<string>();
        var finalDirectory = GetFinalDirectory();
        Directory.CreateDirectory(finalDirectory);

        try
        {
            foreach (var uploadKey in normalizedKeys)
            {
                var metadata = await ReadMetadataAsync(userId, uploadKey, cancellationToken);
                if (metadata == null)
                {
                    return new ErrorDataResult<List<ReturnRequestAttachment>>("Yüklenen fotoğraflardan bazıları bulunamadı. Lütfen tekrar yükleyin.");
                }

                var tempFilePath = Path.Combine(GetTempDirectory(userId), metadata.StoredFileName);
                if (!File.Exists(tempFilePath))
                {
                    return new ErrorDataResult<List<ReturnRequestAttachment>>("Yüklenen fotoğraflardan bazıları bulunamadı. Lütfen tekrar yükleyin.");
                }

                var finalStoredFileName = $"{Guid.NewGuid():N}{Path.GetExtension(metadata.StoredFileName)}";
                var finalFilePath = Path.Combine(finalDirectory, finalStoredFileName);
                File.Move(tempFilePath, finalFilePath, overwrite: false);
                finalizedFilePaths.Add(finalFilePath);

                var metadataPath = GetMetadataPath(tempFilePath);
                if (File.Exists(metadataPath))
                {
                    File.Delete(metadataPath);
                }

                finalizedAttachments.Add(new ReturnRequestAttachment
                {
                    OriginalFileName = metadata.OriginalFileName,
                    StoredFileName = finalStoredFileName,
                    RelativePath = Path.GetRelativePath(_storageRootPath, finalFilePath).Replace('\\', '/'),
                    ContentType = metadata.ContentType,
                    SizeBytes = metadata.SizeBytes
                });
            }
        }
        catch
        {
            foreach (var finalizedFilePath in finalizedFilePaths)
            {
                if (File.Exists(finalizedFilePath))
                {
                    File.Delete(finalizedFilePath);
                }
            }

            throw;
        }

        return new SuccessDataResult<List<ReturnRequestAttachment>>(finalizedAttachments);
    }

    private string? ValidateUpload(ReturnAttachmentUploadContent upload)
    {
        if (upload.SizeBytes <= 0)
        {
            return "Boş dosya yüklenemez.";
        }

        if (upload.SizeBytes > _settings.MaxFileSizeBytes)
        {
            return $"Her fotoğraf en fazla {_settings.MaxFileSizeBytes / (1024 * 1024)} MB olabilir.";
        }

        if (string.IsNullOrWhiteSpace(upload.ContentType) ||
            !_settings.AllowedContentTypes.Contains(upload.ContentType.Trim(), StringComparer.OrdinalIgnoreCase))
        {
            return "Sadece JPEG, PNG, WEBP veya HEIC görseller yüklenebilir.";
        }

        return null;
    }

    private async Task<TemporaryReturnAttachmentMetadata?> ReadMetadataAsync(int userId, string uploadKey, CancellationToken cancellationToken)
    {
        var tempDirectory = GetTempDirectory(userId);
        if (!Directory.Exists(tempDirectory))
        {
            return null;
        }

        var metadataFilePath = Path.Combine(tempDirectory, $"{uploadKey}{TempMetadataExtension}");
        if (!File.Exists(metadataFilePath))
        {
            return null;
        }

        var metadataJson = await File.ReadAllTextAsync(metadataFilePath, cancellationToken);
        var metadata = JsonSerializer.Deserialize<TemporaryReturnAttachmentMetadata>(metadataJson);
        if (metadata == null)
        {
            return null;
        }

        metadata.StoredFileName = metadata.StoredFileName.Trim();
        metadata.OriginalFileName = Path.GetFileName(metadata.OriginalFileName);

        return metadata;
    }

    private string GetTempDirectory(int userId)
    {
        return Path.Combine(_storageRootPath, "temp", userId.ToString());
    }

    private string GetFinalDirectory()
    {
        return Path.Combine(_storageRootPath, "final", DateTime.UtcNow.ToString("yyyy"), DateTime.UtcNow.ToString("MM"));
    }

    private static string GetMetadataPath(string filePath)
    {
        var directory = Path.GetDirectoryName(filePath) ?? string.Empty;
        var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(filePath);
        return Path.Combine(directory, $"{fileNameWithoutExtension}{TempMetadataExtension}");
    }

    private sealed class TemporaryReturnAttachmentMetadata
    {
        public string UploadKey { get; set; } = string.Empty;
        public string OriginalFileName { get; set; } = string.Empty;
        public string StoredFileName { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public long SizeBytes { get; set; }
    }
}
