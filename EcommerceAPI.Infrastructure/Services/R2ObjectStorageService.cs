using Amazon.S3;
using Amazon.S3.Model;
using EcommerceAPI.Core.Interfaces;
using EcommerceAPI.Infrastructure.Settings;
using Microsoft.Extensions.Options;
using System.Net;

namespace EcommerceAPI.Infrastructure.Services;

public class R2ObjectStorageService : IObjectStorageService
{
    private readonly CloudflareR2Settings _settings;
    private readonly IAmazonS3 _s3Client;

    public R2ObjectStorageService(IOptions<CloudflareR2Settings> settings)
    {
        _settings = settings.Value;

        var config = new AmazonS3Config
        {
            ServiceURL = $"https://{_settings.AccountId}.r2.cloudflarestorage.com",
            ForcePathStyle = true,
            SignatureVersion = "4"
        };

        _s3Client = new AmazonS3Client(_settings.AccessKeyId, _settings.SecretAccessKey, config);
    }

    public Task<PresignedUploadUrlResult> GeneratePresignedUploadUrlAsync(
        string objectKey,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        EnsureConfigured();

        if (string.IsNullOrWhiteSpace(objectKey))
        {
            throw new ArgumentException("Object key boş olamaz", nameof(objectKey));
        }

        if (string.IsNullOrWhiteSpace(contentType))
        {
            throw new ArgumentException("Content type boş olamaz", nameof(contentType));
        }

        var normalizedKey = NormalizeObjectKey(objectKey);
        var expirySeconds = _settings.PresignedUrlExpirySeconds > 0 ? _settings.PresignedUrlExpirySeconds : 300;

        var request = new GetPreSignedUrlRequest
        {
            BucketName = _settings.BucketName,
            Key = normalizedKey,
            Verb = HttpVerb.PUT,
            Expires = DateTime.UtcNow.AddSeconds(expirySeconds),
            ContentType = contentType
        };

        var uploadUrl = _s3Client.GetPreSignedURL(request);
        var publicUrl = GetPublicUrl(normalizedKey);

        return Task.FromResult(new PresignedUploadUrlResult(uploadUrl, publicUrl, normalizedKey));
    }

    public async Task<bool> ExistsAsync(string objectKey, CancellationToken cancellationToken = default)
    {
        EnsureConfigured();

        if (string.IsNullOrWhiteSpace(objectKey))
        {
            return false;
        }

        var normalizedKey = NormalizeObjectKey(objectKey);

        try
        {
            await _s3Client.GetObjectMetadataAsync(
                new GetObjectMetadataRequest
                {
                    BucketName = _settings.BucketName,
                    Key = normalizedKey
                },
                cancellationToken);

            return true;
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return false;
        }
    }

    public async Task<IReadOnlyList<ObjectStorageObjectInfo>> ListObjectsAsync(
        string? prefix = null,
        int? maxKeys = null,
        CancellationToken cancellationToken = default)
    {
        EnsureConfigured();

        var normalizedPrefix = string.IsNullOrWhiteSpace(prefix) ? null : NormalizeObjectKey(prefix);
        var remaining = maxKeys.HasValue && maxKeys.Value > 0
            ? maxKeys.Value
            : int.MaxValue;

        var objects = new List<ObjectStorageObjectInfo>();
        string? continuationToken = null;

        do
        {
            var request = new ListObjectsV2Request
            {
                BucketName = _settings.BucketName,
                Prefix = normalizedPrefix,
                ContinuationToken = continuationToken,
                MaxKeys = Math.Min(1000, remaining)
            };

            var response = await _s3Client.ListObjectsV2Async(request, cancellationToken);

            foreach (var item in response.S3Objects)
            {
                if (string.IsNullOrWhiteSpace(item.Key))
                {
                    continue;
                }

                objects.Add(new ObjectStorageObjectInfo(
                    NormalizeObjectKey(item.Key),
                    item.LastModified.ToUniversalTime()));

                remaining--;
                if (remaining <= 0)
                {
                    break;
                }
            }

            if (remaining <= 0 || !response.IsTruncated)
            {
                break;
            }

            continuationToken = response.NextContinuationToken;
        } while (!string.IsNullOrWhiteSpace(continuationToken));

        return objects;
    }

    public async Task DeleteAsync(string objectKey, CancellationToken cancellationToken = default)
    {
        EnsureConfigured();

        if (string.IsNullOrWhiteSpace(objectKey))
        {
            return;
        }

        var normalizedKey = NormalizeObjectKey(objectKey);
        await _s3Client.DeleteObjectAsync(
            new DeleteObjectRequest
            {
                BucketName = _settings.BucketName,
                Key = normalizedKey
            },
            cancellationToken);
    }

    public string GetPublicUrl(string objectKey)
    {
        EnsureConfigured();

        var normalizedKey = NormalizeObjectKey(objectKey);
        return $"{_settings.PublicBaseUrl.TrimEnd('/')}/{normalizedKey}";
    }

    private static string NormalizeObjectKey(string objectKey)
    {
        return objectKey.Trim().TrimStart('/');
    }

    private void EnsureConfigured()
    {
        if (string.IsNullOrWhiteSpace(_settings.AccountId))
        {
            throw new InvalidOperationException("CloudflareR2:AccountId ayarı zorunludur");
        }

        if (string.IsNullOrWhiteSpace(_settings.AccessKeyId))
        {
            throw new InvalidOperationException("CloudflareR2:AccessKeyId ayarı zorunludur");
        }

        if (string.IsNullOrWhiteSpace(_settings.SecretAccessKey))
        {
            throw new InvalidOperationException("CloudflareR2:SecretAccessKey ayarı zorunludur");
        }

        if (string.IsNullOrWhiteSpace(_settings.BucketName))
        {
            throw new InvalidOperationException("CloudflareR2:BucketName ayarı zorunludur");
        }

        if (string.IsNullOrWhiteSpace(_settings.PublicBaseUrl))
        {
            throw new InvalidOperationException("CloudflareR2:PublicBaseUrl ayarı zorunludur");
        }
    }
}
