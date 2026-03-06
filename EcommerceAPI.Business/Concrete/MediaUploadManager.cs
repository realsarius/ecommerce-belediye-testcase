using EcommerceAPI.Application.Abstractions.ServiceContracts;
using EcommerceAPI.Business.Validators;
using EcommerceAPI.Core.Aspects.Autofac.Transaction;
using EcommerceAPI.Core.Aspects.Autofac.Validation;
using EcommerceAPI.Core.Interfaces;
using EcommerceAPI.Core.Utilities.Results;
using EcommerceAPI.Core.Utilities.Storage;
using EcommerceAPI.Application.Abstractions.Persistence;
using EcommerceAPI.Entities.Concrete;
using EcommerceAPI.Entities.DTOs;
using EcommerceAPI.Entities.IntegrationEvents;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace EcommerceAPI.Business.Concrete;

public class MediaUploadManager : IMediaUploadService
{
    private const long MaxFileSizeBytes = 10 * 1024 * 1024;

    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg",
        "image/png",
        "image/webp",
        "image/gif"
    };

    private readonly IObjectStorageService _objectStorageService;
    private readonly IProductDal _productDal;
    private readonly ICategoryDal _categoryDal;
    private readonly ISellerProfileDal _sellerProfileDal;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<MediaUploadManager> _logger;

    public MediaUploadManager(
        IObjectStorageService objectStorageService,
        IProductDal productDal,
        ICategoryDal categoryDal,
        ISellerProfileDal sellerProfileDal,
        IUnitOfWork unitOfWork,
        IPublishEndpoint publishEndpoint,
        ILogger<MediaUploadManager> logger)
    {
        _objectStorageService = objectStorageService;
        _productDal = productDal;
        _categoryDal = categoryDal;
        _sellerProfileDal = sellerProfileDal;
        _unitOfWork = unitOfWork;
        _publishEndpoint = publishEndpoint;
        _logger = logger;
    }

    [ValidationAspect(typeof(PresignMediaUploadRequestValidator))]
    public async Task<IDataResult<PresignedMediaUploadDto>> GetPresignedUploadUrlAsync(
        int userId,
        bool isAdmin,
        PresignMediaUploadRequest request)
    {
        var contextResult = ParseContext(request.Context);
        if (!contextResult.Success)
        {
            return new ErrorDataResult<PresignedMediaUploadDto>(contextResult.Message);
        }

        if (!AllowedContentTypes.Contains(request.ContentType))
        {
            return new ErrorDataResult<PresignedMediaUploadDto>("Sadece JPEG, PNG, WebP veya GIF yüklenebilir");
        }

        if (request.FileSizeBytes <= 0)
        {
            return new ErrorDataResult<PresignedMediaUploadDto>("Dosya boyutu geçersiz");
        }

        if (request.FileSizeBytes > MaxFileSizeBytes)
        {
            return new ErrorDataResult<PresignedMediaUploadDto>("Dosya boyutu 10 MB sınırını aşıyor");
        }

        var extension = ResolveExtensionFromContentType(request.ContentType);
        var objectKeyResult = await BuildObjectKeyAsync(contextResult.Data, userId, isAdmin, request.ReferenceId, extension);
        if (!objectKeyResult.Success)
        {
            return new ErrorDataResult<PresignedMediaUploadDto>(objectKeyResult.Message);
        }

        try
        {
            var presigned = await _objectStorageService.GeneratePresignedUploadUrlAsync(
                objectKeyResult.Data,
                request.ContentType);

            return new SuccessDataResult<PresignedMediaUploadDto>(new PresignedMediaUploadDto
            {
                UploadUrl = presigned.UploadUrl,
                PublicUrl = presigned.PublicUrl,
                ObjectKey = presigned.ObjectKey
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Presigned URL üretilemedi. Context={Context}, UserId={UserId}", request.Context, userId);
            return new ErrorDataResult<PresignedMediaUploadDto>("Yükleme bağlantısı oluşturulamadı");
        }
    }

    [TransactionScopeAspect]
    [ValidationAspect(typeof(ConfirmMediaUploadRequestValidator))]
    public async Task<IDataResult<ConfirmMediaUploadDto>> ConfirmUploadAsync(
        int userId,
        bool isAdmin,
        ConfirmMediaUploadRequest request)
    {
        var contextResult = ParseContext(request.Context);
        if (!contextResult.Success)
        {
            return new ErrorDataResult<ConfirmMediaUploadDto>(contextResult.Message);
        }

        var objectKey = request.ObjectKey.Trim().TrimStart('/');
        if (!IsValidObjectKey(objectKey))
        {
            return new ErrorDataResult<ConfirmMediaUploadDto>("Geçersiz object key formatı");
        }

        if (!await _objectStorageService.ExistsAsync(objectKey))
        {
            return new ErrorDataResult<ConfirmMediaUploadDto>("Yüklenen dosya storage üzerinde bulunamadı");
        }

        var fileHeader = await _objectStorageService.GetObjectHeaderBytesAsync(objectKey, maxBytes: 32);
        if (!TryDetectImageContentType(fileHeader, out var detectedContentType) || !AllowedContentTypes.Contains(detectedContentType))
        {
            _logger.LogWarning(
                "Upload confirm blocked due to invalid image signature. ObjectKey={ObjectKey}, DetectedContentType={DetectedContentType}",
                objectKey,
                string.IsNullOrWhiteSpace(detectedContentType) ? "unknown" : detectedContentType);
            return new ErrorDataResult<ConfirmMediaUploadDto>("Yüklenen dosya geçerli bir görsel değil");
        }

        var context = contextResult.Data;

        if (context == MediaUploadContext.Product)
        {
            return await ConfirmProductImageAsync(userId, isAdmin, request, objectKey);
        }

        if (context == MediaUploadContext.Category)
        {
            return await ConfirmCategoryImageAsync(isAdmin, request, objectKey);
        }

        return await ConfirmSellerProfileImageAsync(userId, isAdmin, context, request, objectKey);
    }

    [TransactionScopeAspect]
    public async Task<IResult> DeleteProductImageAsync(int userId, bool isAdmin, int imageId)
    {
        if (imageId <= 0)
        {
            return new ErrorResult("Geçersiz görsel kimliği");
        }

        var product = await _productDal.GetByImageIdForUpdateAsync(imageId);
        if (product == null)
        {
            return new ErrorResult("Silinecek görsel bulunamadı");
        }

        var authorizationResult = await EnsureProductOwnershipAsync(userId, isAdmin, product);
        if (!authorizationResult.Success)
        {
            return authorizationResult;
        }

        var image = product.Images.FirstOrDefault(x => x.Id == imageId);
        if (image == null)
        {
            return new ErrorResult("Silinecek görsel bulunamadı");
        }

        product.Images.Remove(image);

        var sortedImages = product.Images
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Id)
            .ToList();

        if (sortedImages.Count > 0 && sortedImages.All(x => !x.IsPrimary))
        {
            sortedImages[0].IsPrimary = true;
        }

        for (var index = 0; index < sortedImages.Count; index++)
        {
            sortedImages[index].SortOrder = index;
        }

        _productDal.Update(product);
        await QueueProductIndexSyncEventAsync(product.Id, "DeleteProductImage");
        await _unitOfWork.SaveChangesAsync();

        if (!string.IsNullOrWhiteSpace(image.ObjectKey))
        {
            try
            {
                await _objectStorageService.DeleteAsync(image.ObjectKey);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Storage'dan görsel silinemedi. ObjectKey={ObjectKey}", image.ObjectKey);
            }
        }

        return new SuccessResult("Görsel silindi");
    }

    [TransactionScopeAspect]
    [ValidationAspect(typeof(ReorderProductImagesRequestValidator))]
    public async Task<IResult> ReorderProductImagesAsync(
        int userId,
        bool isAdmin,
        int productId,
        ReorderProductImagesRequest request)
    {
        var product = await _productDal.GetByIdForUpdateAsync(productId);
        if (product == null)
        {
            return new ErrorResult("Ürün bulunamadı");
        }

        var authorizationResult = await EnsureProductOwnershipAsync(userId, isAdmin, product);
        if (!authorizationResult.Success)
        {
            return authorizationResult;
        }

        var orderedItems = request.ImageOrders
            .OrderBy(x => x.DisplayOrder)
            .ThenBy(x => x.ImageId)
            .ToList();

        var incomingIds = orderedItems.Select(x => x.ImageId).Distinct().ToList();
        var existingIds = product.Images.Select(x => x.Id).ToHashSet();

        if (incomingIds.Count != product.Images.Count || incomingIds.Any(id => !existingIds.Contains(id)))
        {
            return new ErrorResult("Sıralama için ürünün tüm görselleri gönderilmelidir");
        }

        var primaryId = orderedItems.FirstOrDefault(x => x.IsPrimary)?.ImageId ?? orderedItems.First().ImageId;

        for (var index = 0; index < orderedItems.Count; index++)
        {
            var orderItem = orderedItems[index];
            var image = product.Images.First(x => x.Id == orderItem.ImageId);
            image.SortOrder = index;
            image.IsPrimary = image.Id == primaryId;
        }

        _productDal.Update(product);
        await QueueProductIndexSyncEventAsync(product.Id, "ReorderProductImages");
        await _unitOfWork.SaveChangesAsync();

        return new SuccessResult("Görsel sıralaması güncellendi");
    }

    private async Task<IDataResult<ConfirmMediaUploadDto>> ConfirmProductImageAsync(
        int userId,
        bool isAdmin,
        ConfirmMediaUploadRequest request,
        string objectKey)
    {
        var product = await _productDal.GetByIdForUpdateAsync(request.ReferenceId);
        if (product == null)
        {
            return new ErrorDataResult<ConfirmMediaUploadDto>("Ürün bulunamadı");
        }

        var authorizationResult = await EnsureProductOwnershipAsync(userId, isAdmin, product);
        if (!authorizationResult.Success)
        {
            return new ErrorDataResult<ConfirmMediaUploadDto>(authorizationResult.Message);
        }

        if (!product.SellerId.HasValue)
        {
            return new ErrorDataResult<ConfirmMediaUploadDto>("Ürün satıcı bilgisi eksik olduğu için görsel kaydı yapılamadı");
        }

        var sellerId = product.SellerId.Value;
        var expectedPrefix = $"products/seller-{sellerId}/product-{product.Id}/";
        if (!HasObjectKeyPrefix(objectKey, expectedPrefix))
        {
            return new ErrorDataResult<ConfirmMediaUploadDto>("Geçersiz object key prefix");
        }

        var imageUrl = _objectStorageService.GetPublicUrl(objectKey);

        var sortOrder = request.SortOrder.GetValueOrDefault(
            product.Images.Count == 0 ? 0 : product.Images.Max(x => x.SortOrder) + 1);

        var isPrimary = request.IsPrimary ?? product.Images.Count == 0;
        if (isPrimary)
        {
            foreach (var existingImage in product.Images)
            {
                existingImage.IsPrimary = false;
            }
        }

        var image = new ProductImage
        {
            ProductId = product.Id,
            ImageUrl = imageUrl,
            ObjectKey = objectKey,
            SortOrder = sortOrder,
            IsPrimary = isPrimary
        };

        product.Images.Add(image);
        _productDal.Update(product);
        await QueueProductIndexSyncEventAsync(product.Id, "ConfirmProductImage");
        await _unitOfWork.SaveChangesAsync();

        return new SuccessDataResult<ConfirmMediaUploadDto>(new ConfirmMediaUploadDto
        {
            ImageId = image.Id,
            ImageUrl = image.ImageUrl,
            ObjectKey = image.ObjectKey ?? string.Empty,
            IsPrimary = image.IsPrimary,
            SortOrder = image.SortOrder
        }, "Görsel kaydedildi");
    }

    private async Task<IDataResult<ConfirmMediaUploadDto>> ConfirmCategoryImageAsync(
        bool isAdmin,
        ConfirmMediaUploadRequest request,
        string objectKey)
    {
        if (!isAdmin)
        {
            return new ErrorDataResult<ConfirmMediaUploadDto>("Yetkiniz yok");
        }

        var category = await _categoryDal.GetAsync(c => c.Id == request.ReferenceId);
        if (category == null)
        {
            return new ErrorDataResult<ConfirmMediaUploadDto>("Kategori bulunamadı");
        }

        var expectedPrefix = $"categories/category-{category.Id}/";
        if (!HasObjectKeyPrefix(objectKey, expectedPrefix))
        {
            return new ErrorDataResult<ConfirmMediaUploadDto>("Geçersiz object key prefix");
        }

        category.ImageUrl = _objectStorageService.GetPublicUrl(objectKey);
        category.ImageObjectKey = objectKey;
        category.UpdatedAt = DateTime.UtcNow;

        _categoryDal.Update(category);
        await _unitOfWork.SaveChangesAsync();

        return new SuccessDataResult<ConfirmMediaUploadDto>(new ConfirmMediaUploadDto
        {
            ImageUrl = category.ImageUrl,
            ObjectKey = category.ImageObjectKey
        }, "Kategori görseli güncellendi");
    }

    private async Task<IDataResult<ConfirmMediaUploadDto>> ConfirmSellerProfileImageAsync(
        int userId,
        bool isAdmin,
        MediaUploadContext context,
        ConfirmMediaUploadRequest request,
        string objectKey)
    {
        SellerProfile? profile;

        if (isAdmin && request.ReferenceId > 0)
        {
            profile = await _sellerProfileDal.GetByIdWithDetailsAsync(request.ReferenceId);
        }
        else
        {
            profile = await _sellerProfileDal.GetByUserIdWithDetailsAsync(userId);
        }

        if (profile == null)
        {
            return new ErrorDataResult<ConfirmMediaUploadDto>("Satıcı profili bulunamadı");
        }

        if (!isAdmin && request.ReferenceId > 0 && request.ReferenceId != profile.Id)
        {
            return new ErrorDataResult<ConfirmMediaUploadDto>("Yetkiniz yok");
        }

        var expectedPrefix = context == MediaUploadContext.SellerLogo
            ? $"sellers/seller-{profile.Id}/logo."
            : $"sellers/seller-{profile.Id}/banner.";

        if (!HasObjectKeyPrefix(objectKey, expectedPrefix))
        {
            return new ErrorDataResult<ConfirmMediaUploadDto>("Geçersiz object key prefix");
        }

        var imageUrl = _objectStorageService.GetPublicUrl(objectKey);

        if (context == MediaUploadContext.SellerLogo)
        {
            profile.LogoUrl = imageUrl;
            profile.LogoObjectKey = objectKey;
        }
        else
        {
            profile.BannerImageUrl = imageUrl;
            profile.BannerImageObjectKey = objectKey;
        }

        profile.UpdatedAt = DateTime.UtcNow;
        _sellerProfileDal.Update(profile);
        await _unitOfWork.SaveChangesAsync();

        return new SuccessDataResult<ConfirmMediaUploadDto>(new ConfirmMediaUploadDto
        {
            ImageUrl = imageUrl,
            ObjectKey = objectKey
        }, "Satıcı görseli güncellendi");
    }

    private async Task<IDataResult<string>> BuildObjectKeyAsync(
        MediaUploadContext context,
        int userId,
        bool isAdmin,
        int referenceId,
        string extension)
    {
        if (context == MediaUploadContext.Product)
        {
            if (referenceId <= 0)
            {
                return new ErrorDataResult<string>(message: "Ürün görselleri için geçerli bir ürün kimliği gereklidir");
            }

            var product = await _productDal.GetByIdWithDetailsAsync(referenceId);
            if (product == null)
            {
                return new ErrorDataResult<string>(message: "Ürün bulunamadı");
            }

            if (!isAdmin)
            {
                var sellerProfile = await _sellerProfileDal.GetAsync(profile => profile.UserId == userId);
                if (sellerProfile == null || product.SellerId != sellerProfile.Id)
                {
                    return new ErrorDataResult<string>(message: "Yetkiniz yok");
                }
            }

            if (!product.SellerId.HasValue)
            {
                return new ErrorDataResult<string>(message: "Ürün satıcı bilgisi eksik olduğu için yükleme başlatılamadı");
            }

            var sellerIdForPath = product.SellerId.Value;
            return new SuccessDataResult<string>(data: StorageKeyGenerator.ProductImage(sellerIdForPath, product.Id, extension));
        }

        if (context == MediaUploadContext.Category)
        {
            if (!isAdmin)
            {
                return new ErrorDataResult<string>(message: "Yetkiniz yok");
            }

            if (referenceId <= 0)
            {
                return new ErrorDataResult<string>(message: "Kategori görselleri için geçerli bir kategori kimliği gereklidir");
            }

            var categoryExists = await _categoryDal.ExistsAsync(category => category.Id == referenceId);
            if (!categoryExists)
            {
                return new ErrorDataResult<string>(message: "Kategori bulunamadı");
            }

            return new SuccessDataResult<string>(data: StorageKeyGenerator.CategoryImage(referenceId, extension));
        }

        if (context == MediaUploadContext.SellerLogo || context == MediaUploadContext.SellerBanner)
        {
            SellerProfile? targetProfile;

            if (isAdmin && referenceId > 0)
            {
                targetProfile = await _sellerProfileDal.GetAsync(profile => profile.Id == referenceId);
            }
            else
            {
                targetProfile = await _sellerProfileDal.GetAsync(profile => profile.UserId == userId);
            }

            if (targetProfile == null)
            {
                return new ErrorDataResult<string>(message: "Satıcı profili bulunamadı");
            }

            if (!isAdmin && referenceId > 0 && referenceId != targetProfile.Id)
            {
                return new ErrorDataResult<string>(message: "Yetkiniz yok");
            }

            var objectKey = context == MediaUploadContext.SellerLogo
                ? StorageKeyGenerator.SellerLogo(targetProfile.Id, extension)
                : StorageKeyGenerator.SellerBanner(targetProfile.Id, extension);

            return new SuccessDataResult<string>(data: objectKey);
        }

        return new ErrorDataResult<string>(message: "Desteklenmeyen upload context");
    }

    private async Task<IResult> EnsureProductOwnershipAsync(int userId, bool isAdmin, Product product)
    {
        if (isAdmin)
        {
            return new SuccessResult();
        }

        var sellerProfile = await _sellerProfileDal.GetAsync(profile => profile.UserId == userId);
        if (sellerProfile == null || product.SellerId != sellerProfile.Id)
        {
            return new ErrorResult("Yetkiniz yok");
        }

        return new SuccessResult();
    }

    private async Task QueueProductIndexSyncEventAsync(int productId, string reason)
    {
        await _publishEndpoint.Publish(new ProductIndexSyncEvent
        {
            ProductId = productId,
            Operation = ProductIndexOperations.Upsert,
            Reason = reason
        });
    }

    private static IDataResult<MediaUploadContext> ParseContext(string context)
    {
        var normalized = context?.Trim().ToLowerInvariant();

        return normalized switch
        {
            "product" => new SuccessDataResult<MediaUploadContext>(MediaUploadContext.Product),
            "category" => new SuccessDataResult<MediaUploadContext>(MediaUploadContext.Category),
            "seller-logo" => new SuccessDataResult<MediaUploadContext>(MediaUploadContext.SellerLogo),
            "seller-banner" => new SuccessDataResult<MediaUploadContext>(MediaUploadContext.SellerBanner),
            _ => new ErrorDataResult<MediaUploadContext>("Geçersiz upload context")
        };
    }

    private static string ResolveExtensionFromContentType(string contentType)
    {
        return contentType.Trim().ToLowerInvariant() switch
        {
            "image/jpeg" => "jpg",
            "image/png" => "png",
            "image/webp" => "webp",
            "image/gif" => "gif",
            _ => "bin"
        };
    }

    private static bool HasObjectKeyPrefix(string objectKey, string expectedPrefix)
    {
        return objectKey.StartsWith(expectedPrefix, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsValidObjectKey(string objectKey)
    {
        if (string.IsNullOrWhiteSpace(objectKey))
        {
            return false;
        }

        if (objectKey.Length > 1024 || objectKey.EndsWith("/", StringComparison.Ordinal))
        {
            return false;
        }

        if (objectKey.Contains('\\') || objectKey.Contains("//", StringComparison.Ordinal))
        {
            return false;
        }

        var segments = objectKey.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length == 0)
        {
            return false;
        }

        return segments.All(segment =>
            !string.IsNullOrWhiteSpace(segment) &&
            segment != "." &&
            segment != ".." &&
            !segment.Any(char.IsControl));
    }

    private static bool TryDetectImageContentType(byte[]? fileHeader, out string contentType)
    {
        contentType = string.Empty;

        if (fileHeader == null || fileHeader.Length < 3)
        {
            return false;
        }

        var header = fileHeader.AsSpan();

        if (header.Length >= 3 &&
            header[0] == 0xFF &&
            header[1] == 0xD8 &&
            header[2] == 0xFF)
        {
            contentType = "image/jpeg";
            return true;
        }

        if (header.Length >= 8 &&
            header[0] == 0x89 &&
            header[1] == 0x50 &&
            header[2] == 0x4E &&
            header[3] == 0x47 &&
            header[4] == 0x0D &&
            header[5] == 0x0A &&
            header[6] == 0x1A &&
            header[7] == 0x0A)
        {
            contentType = "image/png";
            return true;
        }

        if (header.Length >= 6 &&
            header[0] == 0x47 &&
            header[1] == 0x49 &&
            header[2] == 0x46 &&
            header[3] == 0x38 &&
            (header[4] == 0x37 || header[4] == 0x39) &&
            header[5] == 0x61)
        {
            contentType = "image/gif";
            return true;
        }

        if (header.Length >= 12 &&
            header[0] == 0x52 &&
            header[1] == 0x49 &&
            header[2] == 0x46 &&
            header[3] == 0x46 &&
            header[8] == 0x57 &&
            header[9] == 0x45 &&
            header[10] == 0x42 &&
            header[11] == 0x50)
        {
            contentType = "image/webp";
            return true;
        }

        return false;
    }

    private enum MediaUploadContext
    {
        Product,
        Category,
        SellerLogo,
        SellerBanner
    }
}
