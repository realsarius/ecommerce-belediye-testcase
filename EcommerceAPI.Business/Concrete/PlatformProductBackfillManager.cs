using EcommerceAPI.Application.Abstractions.ServiceContracts;
using EcommerceAPI.Core.Interfaces;
using EcommerceAPI.Core.Utilities.Results;
using EcommerceAPI.Application.Abstractions.Persistence;
using Microsoft.Extensions.Logging;

namespace EcommerceAPI.Business.Concrete;

public class PlatformProductBackfillManager : IPlatformProductBackfillService
{
    private readonly IProductDal _productDal;
    private readonly IPlatformSellerService _platformSellerService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<PlatformProductBackfillManager> _logger;

    public PlatformProductBackfillManager(
        IProductDal productDal,
        IPlatformSellerService platformSellerService,
        IUnitOfWork unitOfWork,
        ILogger<PlatformProductBackfillManager> logger)
    {
        _productDal = productDal;
        _platformSellerService = platformSellerService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public Task<IReadOnlyList<int>> GetProductIdsWithoutSellerSnapshotAsync()
    {
        return _productDal.GetProductIdsWithoutSellerAsync();
    }

    public async Task<IResult> BackfillMissingSellerIdsAsync()
    {
        var missingSellerCountBefore = await _productDal.CountProductsWithoutSellerAsync();
        if (missingSellerCountBefore == 0)
        {
            _logger.LogInformation("Platform product backfill atlandı. SellerId eksik urun bulunmuyor");
            return new SuccessResult("SellerId eksik urun bulunmuyor");
        }

        var platformSellerResult = await _platformSellerService.GetOrCreatePlatformSellerIdAsync();
        if (!platformSellerResult.Success)
        {
            _logger.LogWarning(
                "Platform product backfill baslatilamadi. Platform seller hazir degil. Message={Message}",
                platformSellerResult.Message);
            return new ErrorResult("Platform satıcı hazırlığı tamamlanamadığı için backfill çalıştırılamadı");
        }

        try
        {
            await _unitOfWork.BeginTransactionAsync();

            var updatedCount = await _productDal.BackfillMissingSellerIdsAsync(
                platformSellerResult.Data,
                DateTime.UtcNow);

            var missingSellerCountAfter = await _productDal.CountProductsWithoutSellerAsync();

            await _unitOfWork.CommitTransactionAsync();

            _logger.LogInformation(
                "Platform product backfill tamamlandi. Before={BeforeCount}, Updated={UpdatedCount}, After={AfterCount}, PlatformSellerId={PlatformSellerId}",
                missingSellerCountBefore,
                updatedCount,
                missingSellerCountAfter,
                platformSellerResult.Data);

            return new SuccessResult("SellerId eksik urunler platform saticiya baglandi");
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync();
            _logger.LogError(ex, "Platform product backfill sırasında hata olustu");
            return new ErrorResult("SellerId backfill işlemi sırasında hata oluştu");
        }
    }
}
