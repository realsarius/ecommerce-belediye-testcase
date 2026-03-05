using EcommerceAPI.Business.Abstract;
using EcommerceAPI.Core.Interfaces;
using EcommerceAPI.DataAccess.Abstract;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace EcommerceAPI.Business.Concrete;

public class OrphanMediaCleanupManager : IOrphanMediaCleanupService
{
    private readonly IObjectStorageService _objectStorageService;
    private readonly IProductDal _productDal;
    private readonly ICategoryDal _categoryDal;
    private readonly ISellerProfileDal _sellerProfileDal;
    private readonly ILogger<OrphanMediaCleanupManager> _logger;
    private readonly int _graceHours;
    private readonly int _maxScanPerRun;
    private readonly int _maxDeletePerRun;

    public OrphanMediaCleanupManager(
        IObjectStorageService objectStorageService,
        IProductDal productDal,
        ICategoryDal categoryDal,
        ISellerProfileDal sellerProfileDal,
        IConfiguration configuration,
        ILogger<OrphanMediaCleanupManager> logger)
    {
        _objectStorageService = objectStorageService;
        _productDal = productDal;
        _categoryDal = categoryDal;
        _sellerProfileDal = sellerProfileDal;
        _logger = logger;

        _graceHours = Clamp(configuration.GetValue("CloudflareR2:OrphanCleanupGraceHours", 24), 1, 24 * 14);
        _maxScanPerRun = Clamp(configuration.GetValue("CloudflareR2:OrphanCleanupMaxScanPerRun", 5000), 100, 20000);
        _maxDeletePerRun = Clamp(configuration.GetValue("CloudflareR2:OrphanCleanupMaxDeletePerRun", 500), 50, 5000);
    }

    public async Task ExecuteAsync()
    {
        var cutoffUtc = DateTime.UtcNow.AddHours(-_graceHours);

        IReadOnlyList<ObjectStorageObjectInfo> storageObjects;
        try
        {
            storageObjects = await _objectStorageService.ListObjectsAsync(maxKeys: _maxScanPerRun);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Orphan media cleanup skipped. Storage konfigürasyonu eksik olabilir");
            return;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Orphan media cleanup sırasında object listesi alınamadı");
            return;
        }

        if (storageObjects.Count == 0)
        {
            _logger.LogInformation("Orphan media cleanup tamamlandı. Storage tarafında dosya bulunamadı");
            return;
        }

        var referencedKeys = await LoadReferencedObjectKeysAsync();
        var orphanCandidates = storageObjects
            .Where(item => item.LastModifiedUtc <= cutoffUtc)
            .Where(item => !referencedKeys.Contains(item.ObjectKey))
            .Take(_maxDeletePerRun)
            .ToList();

        if (orphanCandidates.Count == 0)
        {
            _logger.LogInformation(
                "Orphan media cleanup tamamlandı. Orphan dosya yok. Scanned={Scanned}, Referenced={Referenced}, CutoffUtc={CutoffUtc}",
                storageObjects.Count,
                referencedKeys.Count,
                cutoffUtc);
            return;
        }

        var deletedCount = 0;
        foreach (var orphan in orphanCandidates)
        {
            try
            {
                await _objectStorageService.DeleteAsync(orphan.ObjectKey);
                deletedCount++;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Orphan object silinemedi. ObjectKey={ObjectKey}", orphan.ObjectKey);
            }
        }

        _logger.LogInformation(
            "Orphan media cleanup tamamlandı. Deleted={Deleted}, Candidates={Candidates}, Scanned={Scanned}, Referenced={Referenced}, CutoffUtc={CutoffUtc}",
            deletedCount,
            orphanCandidates.Count,
            storageObjects.Count,
            referencedKeys.Count,
            cutoffUtc);
    }

    private async Task<HashSet<string>> LoadReferencedObjectKeysAsync()
    {
        var keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var productImageKeys = await _productDal.GetAllImageObjectKeysAsync();
        foreach (var key in productImageKeys)
        {
            AddNormalizedKey(keys, key);
        }

        var categoriesWithImages = await _categoryDal.GetListAsync(category =>
            category.ImageObjectKey != null && category.ImageObjectKey != string.Empty);
        foreach (var category in categoriesWithImages)
        {
            AddNormalizedKey(keys, category.ImageObjectKey);
        }

        var sellersWithImages = await _sellerProfileDal.GetListAsync(profile =>
            (profile.LogoObjectKey != null && profile.LogoObjectKey != string.Empty) ||
            (profile.BannerImageObjectKey != null && profile.BannerImageObjectKey != string.Empty));

        foreach (var profile in sellersWithImages)
        {
            AddNormalizedKey(keys, profile.LogoObjectKey);
            AddNormalizedKey(keys, profile.BannerImageObjectKey);
        }

        return keys;
    }

    private static void AddNormalizedKey(HashSet<string> keys, string? objectKey)
    {
        if (string.IsNullOrWhiteSpace(objectKey))
        {
            return;
        }

        keys.Add(objectKey.Trim().TrimStart('/'));
    }

    private static int Clamp(int value, int min, int max)
    {
        if (value < min)
        {
            return min;
        }

        if (value > max)
        {
            return max;
        }

        return value;
    }
}
