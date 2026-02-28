using EcommerceAPI.Business.Abstract;
using EcommerceAPI.Business.Constants;
using EcommerceAPI.Business.Validators;
using EcommerceAPI.Core.Aspects.Autofac.Logging;
using EcommerceAPI.Core.Aspects.Autofac.Transaction;
using EcommerceAPI.Core.Aspects.Autofac.Validation;
using EcommerceAPI.Core.CrossCuttingConcerns.Logging;
using EcommerceAPI.Core.Interfaces;
using EcommerceAPI.Core.Utilities.Results;
using EcommerceAPI.DataAccess.Abstract;
using EcommerceAPI.Entities.Concrete;
using EcommerceAPI.Entities.DTOs;
using EcommerceAPI.Entities.IntegrationEvents;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace EcommerceAPI.Business.Concrete;

public class WishlistPriceAlertManager : IWishlistPriceAlertService
{
    private readonly IPriceAlertDal _priceAlertDal;
    private readonly IProductDal _productDal;
    private readonly IWishlistDal _wishlistDal;
    private readonly IWishlistItemDal _wishlistItemDal;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditService _auditService;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<WishlistPriceAlertManager> _logger;

    public WishlistPriceAlertManager(
        IPriceAlertDal priceAlertDal,
        IProductDal productDal,
        IWishlistDal wishlistDal,
        IWishlistItemDal wishlistItemDal,
        IUnitOfWork unitOfWork,
        IAuditService auditService,
        IPublishEndpoint publishEndpoint,
        ILogger<WishlistPriceAlertManager> logger)
    {
        _priceAlertDal = priceAlertDal;
        _productDal = productDal;
        _wishlistDal = wishlistDal;
        _wishlistItemDal = wishlistItemDal;
        _unitOfWork = unitOfWork;
        _auditService = auditService;
        _publishEndpoint = publishEndpoint;
        _logger = logger;
    }

    public async Task<IDataResult<List<WishlistPriceAlertDto>>> GetUserPriceAlertsAsync(int userId)
    {
        var alerts = await _priceAlertDal.GetUserAlertsWithProductsAsync(userId);
        var result = alerts
            .Where(alert => alert.Product != null)
            .Select(alert => MapToDto(alert))
            .ToList();

        return new SuccessDataResult<List<WishlistPriceAlertDto>>(result);
    }

    [LogAspect]
    [TransactionScopeAspect]
    [ValidationAspect(typeof(UpsertWishlistPriceAlertRequestValidator))]
    public async Task<IDataResult<WishlistPriceAlertDto>> UpsertPriceAlertAsync(int userId, UpsertWishlistPriceAlertRequest request)
    {
        var product = await _productDal.GetAsync(p => p.Id == request.ProductId);
        if (product == null || !product.IsActive)
        {
            return new ErrorDataResult<WishlistPriceAlertDto>(Messages.ProductNotFound);
        }

        var wishlist = await _wishlistDal.GetAsync(w => w.UserId == userId);
        if (wishlist == null || !await _wishlistItemDal.ExistsAsync(wi => wi.WishlistId == wishlist.Id && wi.ProductId == request.ProductId))
        {
            return new ErrorDataResult<WishlistPriceAlertDto>("Fiyat alarmı kurabilmek için ürün favorilerinizde olmalıdır.");
        }

        if (request.TargetPrice >= product.Price)
        {
            return new ErrorDataResult<WishlistPriceAlertDto>("Hedef fiyat mevcut ürün fiyatından düşük olmalıdır.");
        }

        var existingAlert = await _priceAlertDal.GetByUserAndProductAsync(userId, request.ProductId);
        var now = DateTime.UtcNow;

        if (existingAlert == null)
        {
            existingAlert = new PriceAlert
            {
                UserId = userId,
                ProductId = request.ProductId,
                TargetPrice = request.TargetPrice,
                LastKnownPrice = product.Price,
                IsActive = true,
                CreatedAt = now,
                UpdatedAt = now
            };

            await _priceAlertDal.AddAsync(existingAlert);
        }
        else
        {
            existingAlert.TargetPrice = request.TargetPrice;
            existingAlert.LastKnownPrice = product.Price;
            existingAlert.LastTriggeredPrice = null;
            existingAlert.LastNotifiedAt = null;
            existingAlert.IsActive = true;
            existingAlert.UpdatedAt = now;
            _priceAlertDal.Update(existingAlert);
        }

        await _unitOfWork.SaveChangesAsync();

        await _auditService.LogActionAsync(
            userId.ToString(),
            "WishlistPriceAlertUpserted",
            "PriceAlert",
            new
            {
                request.ProductId,
                request.TargetPrice,
                CurrentPrice = product.Price
            });

        return new SuccessDataResult<WishlistPriceAlertDto>(MapToDto(existingAlert, product), "Fiyat alarmı kaydedildi.");
    }

    [LogAspect]
    [TransactionScopeAspect]
    public async Task<IResult> RemovePriceAlertAsync(int userId, int productId)
    {
        var alert = await _priceAlertDal.GetByUserAndProductAsync(userId, productId);
        if (alert == null || !alert.IsActive)
        {
            return new ErrorResult("Aktif fiyat alarmı bulunamadı.");
        }

        alert.IsActive = false;
        alert.UpdatedAt = DateTime.UtcNow;
        _priceAlertDal.Update(alert);

        await _unitOfWork.SaveChangesAsync();
        await _auditService.LogActionAsync(
            userId.ToString(),
            "WishlistPriceAlertRemoved",
            "PriceAlert",
            new { productId });

        return new SuccessResult("Fiyat alarmı kaldırıldı.");
    }

    public async Task ProcessPriceAlertsAsync()
    {
        var alerts = await _priceAlertDal.GetActiveAlertsWithProductsAsync();
        if (alerts.Count == 0)
        {
            return;
        }

        var hasChanges = false;

        foreach (var alert in alerts)
        {
            var product = alert.Product;
            if (product == null || !product.IsActive)
            {
                continue;
            }

            var currentPrice = product.Price;
            var previousPrice = alert.LastKnownPrice > 0 ? alert.LastKnownPrice : currentPrice;
            var priceDropped = currentPrice < previousPrice;
            var shouldTrigger = priceDropped
                && currentPrice <= alert.TargetPrice
                && alert.LastTriggeredPrice != currentPrice;

            if (shouldTrigger)
            {
                await _publishEndpoint.Publish(new WishlistProductPriceDropEvent
                {
                    UserId = alert.UserId,
                    ProductId = alert.ProductId,
                    ProductName = product.Name,
                    TargetPrice = alert.TargetPrice,
                    OldPrice = previousPrice,
                    NewPrice = currentPrice,
                    Currency = product.Currency
                });

                alert.LastTriggeredPrice = currentPrice;
                alert.LastNotifiedAt = DateTime.UtcNow;
                hasChanges = true;

                _logger.LogInformation(
                    "Wishlist analytics event. AnalyticsStream={AnalyticsStream}, AnalyticsEvent={AnalyticsEvent}, UserId={UserId}, ProductId={ProductId}, ProductName={ProductName}, Category={Category}, OldPrice={OldPrice}, NewPrice={NewPrice}, TargetPrice={TargetPrice}, Currency={Currency}, OccurredAt={OccurredAt}",
                    "Wishlist",
                    "WishlistPriceAlertTriggered",
                    alert.UserId,
                    alert.ProductId,
                    product.Name,
                    product.Category?.Name,
                    previousPrice,
                    currentPrice,
                    alert.TargetPrice,
                    product.Currency,
                    DateTime.UtcNow);
            }

            if (alert.LastKnownPrice != currentPrice)
            {
                alert.LastKnownPrice = currentPrice;
                alert.UpdatedAt = DateTime.UtcNow;
                hasChanges = true;
            }

            _priceAlertDal.Update(alert);
        }

        if (!hasChanges)
        {
            return;
        }

        await _unitOfWork.SaveChangesAsync();
    }

    private static WishlistPriceAlertDto MapToDto(PriceAlert alert, Product? product = null)
    {
        var resolvedProduct = product ?? alert.Product;

        return new WishlistPriceAlertDto
        {
            Id = alert.Id,
            ProductId = alert.ProductId,
            ProductName = resolvedProduct?.Name ?? string.Empty,
            Currency = resolvedProduct?.Currency ?? "TRY",
            CurrentPrice = resolvedProduct?.Price ?? alert.LastKnownPrice,
            TargetPrice = alert.TargetPrice,
            IsActive = alert.IsActive,
            LastTriggeredPrice = alert.LastTriggeredPrice,
            LastNotifiedAt = alert.LastNotifiedAt,
            CreatedAt = alert.CreatedAt
        };
    }
}
