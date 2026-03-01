using EcommerceAPI.Business.Abstract;
using EcommerceAPI.Core.Aspects.Autofac.Caching;
using EcommerceAPI.Core.Aspects.Autofac.Logging;
using EcommerceAPI.Core.CrossCuttingConcerns.Logging;
using EcommerceAPI.Core.Interfaces;
using EcommerceAPI.Core.Utilities.Results;
using EcommerceAPI.DataAccess.Abstract;
using EcommerceAPI.Entities.Concrete;
using EcommerceAPI.Entities.DTOs;
using EcommerceAPI.Entities.Enums;
using EcommerceAPI.Entities.IntegrationEvents;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace EcommerceAPI.Business.Concrete;

public class CampaignManager : ICampaignService
{
    private readonly ICampaignDal _campaignDal;
    private readonly IProductDal _productDal;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditService _auditService;
    private readonly ILogger<CampaignManager> _logger;
    private readonly IPublishEndpoint _publishEndpoint;

    public CampaignManager(
        ICampaignDal campaignDal,
        IProductDal productDal,
        IUnitOfWork unitOfWork,
        IAuditService auditService,
        ILogger<CampaignManager> logger,
        IPublishEndpoint publishEndpoint)
    {
        _campaignDal = campaignDal;
        _productDal = productDal;
        _unitOfWork = unitOfWork;
        _auditService = auditService;
        _logger = logger;
        _publishEndpoint = publishEndpoint;
    }

    [LogAspect]
    [CacheAspect(duration: 30)]
    public async Task<IDataResult<List<CampaignDto>>> GetAllAsync()
    {
        var campaigns = await _campaignDal.GetAllWithProductsAsync();
        return new SuccessDataResult<List<CampaignDto>>(campaigns.Select(MapToDto).ToList());
    }

    [LogAspect]
    public async Task<IDataResult<CampaignDto>> GetByIdAsync(int id)
    {
        var campaign = await _campaignDal.GetByIdWithProductsAsync(id);
        return campaign == null
            ? new ErrorDataResult<CampaignDto>("Kampanya bulunamadı.")
            : new SuccessDataResult<CampaignDto>(MapToDto(campaign));
    }

    [LogAspect]
    [CacheAspect(duration: 10)]
    public async Task<IDataResult<List<CampaignDto>>> GetActiveAsync()
    {
        var campaigns = await _campaignDal.GetActiveCampaignsAsync(DateTime.UtcNow);
        return new SuccessDataResult<List<CampaignDto>>(campaigns.Select(MapToDto).ToList());
    }

    [LogAspect]
    [CacheRemoveAspect("GetAllAsync,GetActiveAsync")]
    public async Task<IDataResult<CampaignDto>> CreateAsync(CreateCampaignRequest request)
    {
        var productIds = request.Products.Select(x => x.ProductId).Distinct().ToList();
        if (await _campaignDal.HasOverlappingProductCampaignsAsync(productIds, request.StartsAt, request.EndsAt))
        {
            return new ErrorDataResult<CampaignDto>("Aynı ürün için tarihleri çakışan başka bir kampanya zaten aktif veya planlı durumda.");
        }

        var validation = await BuildCampaignProductsAsync(request.Products);
        if (!validation.Success)
        {
            return new ErrorDataResult<CampaignDto>(validation.Message);
        }

        if (request.StartsAt >= request.EndsAt)
        {
            return new ErrorDataResult<CampaignDto>("Kampanya başlangıç tarihi bitiş tarihinden önce olmalıdır.");
        }

        var campaign = new Campaign
        {
            Name = request.Name.Trim(),
            Description = request.Description?.Trim(),
            BadgeText = request.BadgeText?.Trim(),
            Type = request.Type,
            IsEnabled = request.IsEnabled,
            StartsAt = request.StartsAt,
            EndsAt = request.EndsAt,
            Status = DetermineStatus(request.IsEnabled, request.StartsAt, request.EndsAt, DateTime.UtcNow),
            CampaignProducts = validation.Data
        };

        await _campaignDal.AddAsync(campaign);
        await _unitOfWork.SaveChangesAsync();

        await _auditService.LogActionAsync(
            "Admin",
            "CreateCampaign",
            "Campaign",
            new { CampaignId = campaign.Id, campaign.Name, ProductCount = campaign.CampaignProducts.Count });

        var refreshed = await _campaignDal.GetByIdWithProductsAsync(campaign.Id) ?? campaign;
        return new SuccessDataResult<CampaignDto>(MapToDto(refreshed), "Kampanya oluşturuldu.");
    }

    [LogAspect]
    [CacheRemoveAspect("GetAllAsync,GetActiveAsync")]
    public async Task<IDataResult<CampaignDto>> UpdateAsync(int id, UpdateCampaignRequest request)
    {
        var campaign = await _campaignDal.GetByIdWithProductsAsync(id);
        if (campaign == null)
        {
            return new ErrorDataResult<CampaignDto>("Kampanya bulunamadı.");
        }

        if (!string.IsNullOrWhiteSpace(request.Name))
        {
            campaign.Name = request.Name.Trim();
        }

        if (request.Description != null)
        {
            campaign.Description = request.Description.Trim();
        }

        if (request.BadgeText != null)
        {
            campaign.BadgeText = request.BadgeText.Trim();
        }

        if (request.Type.HasValue)
        {
            campaign.Type = request.Type.Value;
        }

        if (request.IsEnabled.HasValue)
        {
            campaign.IsEnabled = request.IsEnabled.Value;
        }

        if (request.StartsAt.HasValue)
        {
            campaign.StartsAt = request.StartsAt.Value;
        }

        if (request.EndsAt.HasValue)
        {
            campaign.EndsAt = request.EndsAt.Value;
        }

        if (campaign.StartsAt >= campaign.EndsAt)
        {
            return new ErrorDataResult<CampaignDto>("Kampanya başlangıç tarihi bitiş tarihinden önce olmalıdır.");
        }

        if (request.Products != null)
        {
            var productIds = request.Products.Select(x => x.ProductId).Distinct().ToList();
            if (await _campaignDal.HasOverlappingProductCampaignsAsync(productIds, campaign.StartsAt, campaign.EndsAt, id))
            {
                return new ErrorDataResult<CampaignDto>("Aynı ürün için tarihleri çakışan başka bir kampanya zaten aktif veya planlı durumda.");
            }

            var productValidation = await BuildCampaignProductsAsync(request.Products);
            if (!productValidation.Success)
            {
                return new ErrorDataResult<CampaignDto>(productValidation.Message);
            }

            campaign.CampaignProducts.Clear();
            foreach (var product in productValidation.Data)
            {
                campaign.CampaignProducts.Add(product);
            }
        }

        campaign.Status = DetermineStatus(campaign.IsEnabled, campaign.StartsAt, campaign.EndsAt, DateTime.UtcNow);
        campaign.UpdatedAt = DateTime.UtcNow;

        _campaignDal.Update(campaign);
        await _unitOfWork.SaveChangesAsync();

        await _auditService.LogActionAsync(
            "Admin",
            "UpdateCampaign",
            "Campaign",
            new { CampaignId = campaign.Id, campaign.Name, campaign.Status });

        var refreshed = await _campaignDal.GetByIdWithProductsAsync(campaign.Id) ?? campaign;
        return new SuccessDataResult<CampaignDto>(MapToDto(refreshed), "Kampanya güncellendi.");
    }

    [LogAspect]
    [CacheRemoveAspect("GetAllAsync,GetActiveAsync")]
    public async Task<IResult> DeleteAsync(int id)
    {
        var campaign = await _campaignDal.GetAsync(x => x.Id == id);
        if (campaign == null)
        {
            return new ErrorResult("Kampanya bulunamadı.");
        }

        _campaignDal.Delete(campaign);
        await _unitOfWork.SaveChangesAsync();

        await _auditService.LogActionAsync(
            "Admin",
            "DeleteCampaign",
            "Campaign",
            new { CampaignId = id, campaign.Name });

        return new SuccessResult("Kampanya silindi.");
    }

    [LogAspect]
    [CacheRemoveAspect("GetAllAsync,GetActiveAsync")]
    public async Task<IResult> ProcessCampaignLifecycleAsync()
    {
        var campaigns = await _campaignDal.GetListAsync();
        var now = DateTime.UtcNow;
        var changedCount = 0;
        var changedCampaignEvents = new List<CampaignStatusChangedEvent>();

        foreach (var campaign in campaigns)
        {
            var previousStatus = campaign.Status;
            var nextStatus = DetermineStatus(campaign.IsEnabled, campaign.StartsAt, campaign.EndsAt, now);
            if (previousStatus == nextStatus)
            {
                continue;
            }

            campaign.Status = nextStatus;
            campaign.UpdatedAt = now;
            _campaignDal.Update(campaign);
            changedCount++;

            changedCampaignEvents.Add(new CampaignStatusChangedEvent
            {
                CampaignId = campaign.Id,
                CampaignName = campaign.Name,
                BadgeText = campaign.BadgeText,
                PreviousStatus = previousStatus,
                CurrentStatus = nextStatus,
                StartsAt = campaign.StartsAt,
                EndsAt = campaign.EndsAt,
                ProductCount = campaign.CampaignProducts.Count,
                OccurredAt = now
            });
        }

        if (changedCount > 0)
        {
            await _unitOfWork.SaveChangesAsync();

            foreach (var eventMessage in changedCampaignEvents)
            {
                await _publishEndpoint.Publish(eventMessage);
            }
        }

        _logger.LogInformation(
            "Campaign lifecycle sync completed. ChangedCount={ChangedCount}, OccurredAt={OccurredAt}",
            changedCount,
            now);

        return new SuccessResult($"{changedCount} kampanya statüsü güncellendi.");
    }

    [LogAspect]
    public async Task<IResult> TrackInteractionAsync(int campaignId, string interactionType, int? productId = null, int? userId = null, string? sessionId = null)
    {
        var normalizedInteraction = interactionType?.Trim().ToLowerInvariant();
        if (normalizedInteraction is not ("impression" or "click"))
        {
            return new ErrorResult("Geçersiz kampanya etkileşim tipi.");
        }

        var campaign = await _campaignDal.GetByIdWithProductsAsync(campaignId);
        if (campaign == null)
        {
            return new ErrorResult("Kampanya bulunamadı.");
        }

        _logger.LogInformation(
            "Campaign analytics event. AnalyticsStream={AnalyticsStream}, AnalyticsEvent={AnalyticsEvent}, CampaignId={CampaignId}, CampaignName={CampaignName}, InteractionType={InteractionType}, ProductId={ProductId}, SessionId={SessionId}, UserId={UserId}, Status={Status}, StartsAt={StartsAt}, EndsAt={EndsAt}, ProductCount={ProductCount}, OccurredAt={OccurredAt}",
            "Campaign",
            normalizedInteraction == "click" ? "CampaignClicked" : "CampaignImpression",
            campaign.Id,
            campaign.Name,
            normalizedInteraction,
            productId,
            sessionId,
            userId,
            campaign.Status,
            campaign.StartsAt,
            campaign.EndsAt,
            campaign.CampaignProducts.Count,
            DateTime.UtcNow);

        return new SuccessResult();
    }

    private async Task<IDataResult<List<CampaignProduct>>> BuildCampaignProductsAsync(IEnumerable<CreateCampaignProductRequest> requests)
    {
        var normalizedRequests = requests
            .GroupBy(x => x.ProductId)
            .Select(x => x.First())
            .ToList();

        if (normalizedRequests.Count == 0)
        {
            return new ErrorDataResult<List<CampaignProduct>>("Kampanyaya en az bir ürün eklenmelidir.");
        }

        var productIds = normalizedRequests.Select(x => x.ProductId).ToList();
        var products = await _productDal.GetByIdsWithInventoryAsync(productIds);
        var productMap = products
            .Where(x => x.IsActive)
            .ToDictionary(x => x.Id);

        if (productMap.Count != productIds.Count)
        {
            return new ErrorDataResult<List<CampaignProduct>>("Kampanya ürünlerinden biri bulunamadı veya aktif değil.");
        }

        var campaignProducts = new List<CampaignProduct>();
        foreach (var request in normalizedRequests)
        {
            var product = productMap[request.ProductId];

            if (request.CampaignPrice <= 0 || request.CampaignPrice >= product.Price)
            {
                return new ErrorDataResult<List<CampaignProduct>>($"Kampanya fiyatı {product.Name} için ürün fiyatından düşük ve pozitif olmalıdır.");
            }

            campaignProducts.Add(new CampaignProduct
            {
                ProductId = product.Id,
                CampaignPrice = request.CampaignPrice,
                OriginalPriceSnapshot = product.Price,
                IsFeatured = request.IsFeatured
            });
        }

        return new SuccessDataResult<List<CampaignProduct>>(campaignProducts);
    }

    private static CampaignStatus DetermineStatus(bool isEnabled, DateTime startsAt, DateTime endsAt, DateTime utcNow)
    {
        if (!isEnabled)
        {
            return CampaignStatus.Draft;
        }

        if (endsAt <= utcNow)
        {
            return CampaignStatus.Ended;
        }

        if (startsAt <= utcNow)
        {
            return CampaignStatus.Active;
        }

        return CampaignStatus.Scheduled;
    }

    private static CampaignDto MapToDto(Campaign campaign)
    {
        return new CampaignDto
        {
            Id = campaign.Id,
            Name = campaign.Name,
            Description = campaign.Description,
            BadgeText = campaign.BadgeText,
            Type = campaign.Type,
            Status = campaign.Status,
            IsEnabled = campaign.IsEnabled,
            StartsAt = campaign.StartsAt,
            EndsAt = campaign.EndsAt,
            Products = campaign.CampaignProducts
                .OrderByDescending(x => x.IsFeatured)
                .ThenBy(x => x.Product?.Name)
                .Select(x => new CampaignProductDto
                {
                    ProductId = x.ProductId,
                    ProductName = x.Product?.Name ?? string.Empty,
                    ProductSku = x.Product?.SKU ?? string.Empty,
                    OriginalPrice = x.OriginalPriceSnapshot,
                    CampaignPrice = x.CampaignPrice,
                    IsFeatured = x.IsFeatured
                })
                .ToList()
        };
    }
}
