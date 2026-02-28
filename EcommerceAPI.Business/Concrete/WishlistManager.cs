using EcommerceAPI.Business.Abstract;
using EcommerceAPI.Business.Constants;
using EcommerceAPI.Business.Mappers;
using EcommerceAPI.Core.Aspects.Autofac.Caching;
using EcommerceAPI.Core.CrossCuttingConcerns.Logging;
using EcommerceAPI.Core.Interfaces;
using EcommerceAPI.Core.Utilities.Results;
using EcommerceAPI.DataAccess.Abstract;
using EcommerceAPI.Entities.Concrete;
using EcommerceAPI.Entities.DTOs;
using EcommerceAPI.Entities.IntegrationEvents;
using MassTransit;
using Microsoft.Extensions.Logging;
using System.Text;

namespace EcommerceAPI.Business.Concrete;

public class WishlistManager : IWishlistService
{
    private const int DefaultWishlistPageSize = 20;
    private const int MaxWishlistPageSize = 50;
    private readonly IWishlistDal _wishlistDal;
    private readonly IWishlistItemDal _wishlistItemDal;
    private readonly IProductDal _productDal;
    private readonly IWishlistMapper _wishlistMapper;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditService _auditService;
    private readonly ILogger<WishlistManager> _logger;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ICartCacheService _cartCacheService;

    public WishlistManager(
        IWishlistDal wishlistDal,
        IWishlistItemDal wishlistItemDal,
        IProductDal productDal,
        IWishlistMapper wishlistMapper,
        IUnitOfWork unitOfWork,
        IAuditService auditService,
        ILogger<WishlistManager> logger,
        IPublishEndpoint publishEndpoint,
        ICartCacheService cartCacheService)
    {
        _wishlistDal = wishlistDal;
        _wishlistItemDal = wishlistItemDal;
        _productDal = productDal;
        _wishlistMapper = wishlistMapper;
        _unitOfWork = unitOfWork;
        _auditService = auditService;
        _logger = logger;
        _publishEndpoint = publishEndpoint;
        _cartCacheService = cartCacheService;
    }

    [CacheAspect(duration: 10)]
    public async Task<IDataResult<WishlistDto>> GetWishlistByUserIdAsync(int userId, string? cursor = null, int? limit = null)
    {
        var wishlist = await _wishlistDal.GetAsync(w => w.UserId == userId);
        if (wishlist == null)
        {
            return new SuccessDataResult<WishlistDto>(new WishlistDto
            {
                UserId = userId,
                Limit = limit ?? 0
            });
        }

        if (string.IsNullOrWhiteSpace(cursor) && !limit.HasValue)
        {
            var items = await _wishlistItemDal.GetListAsync(wi => wi.WishlistId == wishlist.Id);
            wishlist.Items = new List<WishlistItem>();

            foreach (var item in items)
            {
                item.Product = await _productDal.GetAsync(p => p.Id == item.ProductId);
                wishlist.Items.Add(item);
            }

            var fullDto = _wishlistMapper.ToWishlistDto(wishlist);
            fullDto.Limit = fullDto.Items.Count;
            return new SuccessDataResult<WishlistDto>(fullDto);
        }

        var normalizedLimit = NormalizeLimit(limit);
        var (cursorAddedAt, cursorItemId) = TryDecodeCursor(cursor);
        var pagedItems = await _wishlistItemDal.GetPagedByWishlistIdAsync(
            wishlist.Id,
            cursorAddedAt,
            cursorItemId,
            normalizedLimit + 1);

        var hasMore = pagedItems.Count > normalizedLimit;
        var visibleItems = pagedItems.Take(normalizedLimit).ToList();

        var dto = new WishlistDto
        {
            Id = wishlist.Id,
            UserId = wishlist.UserId,
            Limit = normalizedLimit,
            HasMore = hasMore,
            Items = visibleItems.Select(_wishlistMapper.ToWishlistItemDto).ToList()
        };

        if (hasMore && visibleItems.Count > 0)
        {
            var lastVisibleItem = visibleItems[^1];
            dto.NextCursor = EncodeCursor(lastVisibleItem.AddedAt, lastVisibleItem.Id);
        }

        return new SuccessDataResult<WishlistDto>(dto);
    }

    [CacheAspect(duration: 10)]
    public async Task<IDataResult<WishlistShareSettingsDto>> GetShareSettingsAsync(int userId)
    {
        var wishlist = await _wishlistDal.GetAsync(w => w.UserId == userId);
        if (wishlist == null)
        {
            return new SuccessDataResult<WishlistShareSettingsDto>(new WishlistShareSettingsDto());
        }

        return new SuccessDataResult<WishlistShareSettingsDto>(CreateShareSettingsDto(wishlist));
    }

    [CacheRemoveAspect("EcommerceAPI.Business.Abstract.IWishlistService.GetWishlistByUserIdAsync")]
    [CacheRemoveAspect("EcommerceAPI.Business.Abstract.IWishlistService.GetShareSettingsAsync")]
    public async Task<IDataResult<WishlistShareSettingsDto>> EnableSharingAsync(int userId)
    {
        var wishlist = await _wishlistDal.GetOrCreateByUserIdAsync(userId);

        wishlist.IsPublic = true;
        wishlist.ShareToken ??= Guid.NewGuid();
        wishlist.UpdatedAt = DateTime.UtcNow;

        _wishlistDal.Update(wishlist);
        await _unitOfWork.SaveChangesAsync();

        await _auditService.LogActionAsync(
            userId.ToString(),
            "WishlistSharingEnabled",
            "Wishlist",
            new { WishlistId = wishlist.Id, wishlist.ShareToken });

        return new SuccessDataResult<WishlistShareSettingsDto>(
            CreateShareSettingsDto(wishlist),
            "Favori listeniz paylaşılabilir hale getirildi.");
    }

    [CacheRemoveAspect("EcommerceAPI.Business.Abstract.IWishlistService.GetWishlistByUserIdAsync")]
    [CacheRemoveAspect("EcommerceAPI.Business.Abstract.IWishlistService.GetShareSettingsAsync")]
    public async Task<IResult> DisableSharingAsync(int userId)
    {
        var wishlist = await _wishlistDal.GetAsync(w => w.UserId == userId);
        if (wishlist == null)
        {
            return new SuccessResult("Paylaşım zaten kapalı.");
        }

        wishlist.IsPublic = false;
        wishlist.UpdatedAt = DateTime.UtcNow;

        _wishlistDal.Update(wishlist);
        await _unitOfWork.SaveChangesAsync();

        await _auditService.LogActionAsync(
            userId.ToString(),
            "WishlistSharingDisabled",
            "Wishlist",
            new { WishlistId = wishlist.Id, wishlist.ShareToken });

        return new SuccessResult("Favori paylaşımı kapatıldı.");
    }

    [CacheAspect(duration: 10)]
    public async Task<IDataResult<SharedWishlistDto>> GetPublicWishlistByShareTokenAsync(Guid shareToken, string? cursor = null, int? limit = null)
    {
        var wishlist = await _wishlistDal.GetByShareTokenAsync(shareToken);
        if (wishlist == null || !wishlist.IsPublic)
        {
            return new ErrorDataResult<SharedWishlistDto>("Paylaşılan favori listesi bulunamadı.");
        }

        var normalizedLimit = NormalizeLimit(limit);

        if (string.IsNullOrWhiteSpace(cursor) && !limit.HasValue)
        {
            var items = await _wishlistItemDal.GetListAsync(wi => wi.WishlistId == wishlist.Id);
            wishlist.Items = new List<WishlistItem>();

            foreach (var item in items)
            {
                item.Product = await _productDal.GetAsync(p => p.Id == item.ProductId);
                wishlist.Items.Add(item);
            }

            return new SuccessDataResult<SharedWishlistDto>(new SharedWishlistDto
            {
                Id = wishlist.Id,
                OwnerDisplayName = ResolveOwnerDisplayName(wishlist.User),
                Limit = wishlist.Items.Count,
                Items = wishlist.Items.Select(_wishlistMapper.ToWishlistItemDto).ToList()
            });
        }

        var (cursorAddedAt, cursorItemId) = TryDecodeCursor(cursor);
        var pagedItems = await _wishlistItemDal.GetPagedByWishlistIdAsync(
            wishlist.Id,
            cursorAddedAt,
            cursorItemId,
            normalizedLimit + 1);

        var hasMore = pagedItems.Count > normalizedLimit;
        var visibleItems = pagedItems.Take(normalizedLimit).ToList();

        var dto = new SharedWishlistDto
        {
            Id = wishlist.Id,
            OwnerDisplayName = ResolveOwnerDisplayName(wishlist.User),
            Limit = normalizedLimit,
            HasMore = hasMore,
            Items = visibleItems.Select(_wishlistMapper.ToWishlistItemDto).ToList()
        };

        if (hasMore && visibleItems.Count > 0)
        {
            var lastVisibleItem = visibleItems[^1];
            dto.NextCursor = EncodeCursor(lastVisibleItem.AddedAt, lastVisibleItem.Id);
        }

        return new SuccessDataResult<SharedWishlistDto>(dto);
    }

    [CacheRemoveAspect("EcommerceAPI.Business.Abstract.IWishlistService.GetWishlistByUserIdAsync")]
    public async Task<IResult> AddItemToWishlistAsync(int userId, int productId)
    {
        var product = await _productDal.GetAsync(p => p.Id == productId);
        if (product == null)
        {
            return new ErrorResult(Messages.ProductNotFound);
        }

        if (!product.IsActive)
        {
            return new ErrorResult("Bu ürün artık satışta değil.");
        }

        var wishlist = await _wishlistDal.GetOrCreateByUserIdAsync(userId);

        var itemCount = await _wishlistItemDal.CountAsync(wi => wi.WishlistId == wishlist.Id);
        if (itemCount >= 500)
        {
            return new ErrorResult("Favoriler listesi maksimum kapasiteye ulaştı (500 ürün).");
        }

        var now = DateTime.UtcNow;
        var wishlistItemAddedEvent = new WishlistItemAddedEvent
        {
            UserId = userId,
            WishlistId = wishlist.Id,
            ProductId = productId,
            PriceAtTime = product.Price,
            Currency = product.Currency,
            OccurredAt = now
        };
        var newItem = new WishlistItem
        {
            WishlistId = wishlist.Id,
            ProductId = productId,
            AddedAtPrice = product.Price,
            AddedAt = now,
            CreatedAt = now,
            UpdatedAt = now
        };

        await _unitOfWork.BeginTransactionAsync();
        try
        {
            var wasInserted = await _wishlistItemDal.AddIfNotExistsAsync(newItem);
            if (!wasInserted)
            {
                await _unitOfWork.RollbackTransactionAsync();
                return new SuccessResult("Product is already in wishlist.");
            }

            await SyncWishlistCountsAsync(new[] { productId }, saveChanges: false);
            await PublishWishlistEventAsync(wishlistItemAddedEvent);
            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitTransactionAsync();
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync();
            throw;
        }

        await _auditService.LogActionAsync(
            userId.ToString(),
            "WishlistItemAdded",
            "Wishlist",
            new { ProductId = productId, WishlistId = wishlist.Id, AddedAtPrice = product.Price });

        return new SuccessResult("Product added to wishlist.");
    }

    [CacheRemoveAspect("EcommerceAPI.Business.Abstract.IWishlistService.GetWishlistByUserIdAsync")]
    public async Task<IResult> RemoveItemFromWishlistAsync(int userId, int productId)
    {
        var wishlist = await _wishlistDal.GetAsync(w => w.UserId == userId);
        if (wishlist == null)
        {
            return new SuccessResult("Item removed.");
        }

        await _unitOfWork.BeginTransactionAsync();
        try
        {
            var deletedRowCount = await _wishlistItemDal.DeleteByWishlistAndProductAsync(wishlist.Id, productId);
            if (deletedRowCount == 0)
            {
                await _unitOfWork.RollbackTransactionAsync();
                return new SuccessResult("Product removed from wishlist.");
            }

            var wishlistItemRemovedEvent = new WishlistItemRemovedEvent
            {
                UserId = userId,
                WishlistId = wishlist.Id,
                ProductId = productId,
                Reason = "RemoveItem"
            };

            await SyncWishlistCountsAsync(new[] { productId }, saveChanges: false);
            await PublishWishlistEventAsync(wishlistItemRemovedEvent);
            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitTransactionAsync();
            await _auditService.LogActionAsync(
                userId.ToString(),
                "WishlistItemRemoved",
                "Wishlist",
                new { ProductId = productId, WishlistId = wishlist.Id });
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync();
            throw;
        }

        return new SuccessResult("Product removed from wishlist.");
    }

    [CacheRemoveAspect("EcommerceAPI.Business.Abstract.IWishlistService.GetWishlistByUserIdAsync")]
    public async Task<IResult> ClearWishlistAsync(int userId)
    {
        var wishlist = await _wishlistDal.GetAsync(w => w.UserId == userId);
        if (wishlist == null)
        {
            return new SuccessResult("Wishlist is empty.");
        }

        var items = await _wishlistItemDal.GetListAsync(wi => wi.WishlistId == wishlist.Id);
        var affectedProductIds = items.Select(item => item.ProductId).Distinct().ToList();
        var wishlistItemRemovedEvents = affectedProductIds
            .Select(productId => new WishlistItemRemovedEvent
            {
                UserId = userId,
                WishlistId = wishlist.Id,
                ProductId = productId,
                Reason = "ClearWishlist"
            })
            .ToList();

        foreach (var item in items)
        {
            _wishlistItemDal.Delete(item);
        }

        if (affectedProductIds.Count > 0)
        {
            await _unitOfWork.BeginTransactionAsync();
            try
            {
                await _unitOfWork.SaveChangesAsync();
                await SyncWishlistCountsAsync(affectedProductIds, saveChanges: false);

                foreach (var wishlistItemRemovedEvent in wishlistItemRemovedEvents)
                {
                    await PublishWishlistEventAsync(wishlistItemRemovedEvent);
                }

                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitTransactionAsync();
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }

            await _auditService.LogActionAsync(
                userId.ToString(),
                "WishlistCleared",
                "Wishlist",
                new { WishlistId = wishlist.Id, ProductIds = affectedProductIds });
        }

        return new SuccessResult("Wishlist cleared.");
    }

    [CacheRemoveAspect("cart")]
    public async Task<IDataResult<WishlistBulkAddToCartResultDto>> AddAvailableItemsToCartAsync(int userId)
    {
        var wishlist = await _wishlistDal.GetAsync(w => w.UserId == userId);
        if (wishlist == null)
        {
            return new SuccessDataResult<WishlistBulkAddToCartResultDto>(
                new WishlistBulkAddToCartResultDto(),
                "Favorilerinizde sepete eklenecek ürün bulunamadı.");
        }

        var wishlistItems = await _wishlistItemDal.GetListAsync(wi => wi.WishlistId == wishlist.Id);
        if (wishlistItems.Count == 0)
        {
            return new SuccessDataResult<WishlistBulkAddToCartResultDto>(
                new WishlistBulkAddToCartResultDto(),
                "Favorilerinizde sepete eklenecek ürün bulunamadı.");
        }

        var products = await _productDal.GetByIdsWithInventoryAsync(wishlistItems.Select(item => item.ProductId).Distinct().ToList());
        var productsById = products.ToDictionary(product => product.Id);
        var currentCartItems = await _cartCacheService.GetCartItemsAsync(userId);

        var result = new WishlistBulkAddToCartResultDto
        {
            RequestedCount = wishlistItems.Count
        };

        foreach (var wishlistItem in wishlistItems)
        {
            if (!productsById.TryGetValue(wishlistItem.ProductId, out var product) || !product.IsActive)
            {
                result.SkippedItems.Add(CreateSkippedItem(wishlistItem.ProductId, product?.Name, "Ürün artık satışta değil."));
                continue;
            }

            var availableStock = product.Inventory?.QuantityAvailable ?? 0;
            if (availableStock <= 0)
            {
                result.SkippedItems.Add(CreateSkippedItem(product.Id, product.Name, "Ürün stokta yok."));
                continue;
            }

            currentCartItems.TryGetValue(product.Id, out var currentQuantity);
            if (currentQuantity >= availableStock)
            {
                result.SkippedItems.Add(CreateSkippedItem(product.Id, product.Name, "Sepetteki miktar stok sınırına ulaştı."));
                continue;
            }

            await _cartCacheService.IncrementItemQuantityAsync(userId, product.Id, 1);
            currentCartItems[product.Id] = currentQuantity + 1;
            result.AddedCount++;
        }

        result.SkippedCount = result.SkippedItems.Count;

        await _auditService.LogActionAsync(
            userId.ToString(),
            "WishlistBulkAddedToCart",
            "Wishlist",
            new
            {
                result.RequestedCount,
                result.AddedCount,
                result.SkippedCount,
                SkippedItems = result.SkippedItems.Select(item => new { item.ProductId, item.Reason }).ToList()
            });

        return new SuccessDataResult<WishlistBulkAddToCartResultDto>(
            result,
            BuildBulkAddSummaryMessage(result));
    }

    private Task SyncWishlistCountsAsync(params int[] productIds)
    {
        return SyncWishlistCountsAsync((IEnumerable<int>)productIds);
    }

    private async Task SyncWishlistCountsAsync(IEnumerable<int> productIds, bool saveChanges = true)
    {
        var distinctProductIds = productIds
            .Where(productId => productId > 0)
            .Distinct()
            .ToList();

        if (distinctProductIds.Count == 0)
        {
            return;
        }

        foreach (var productId in distinctProductIds)
        {
            var product = await _productDal.GetAsync(p => p.Id == productId);
            if (product == null)
            {
                continue;
            }

            product.WishlistCount = await _wishlistItemDal.CountAsync(wi => wi.ProductId == productId);
            product.UpdatedAt = DateTime.UtcNow;
            _productDal.Update(product);
        }

        if (saveChanges)
        {
            await _unitOfWork.SaveChangesAsync();
        }
    }

    private async Task PublishWishlistEventAsync<TEvent>(TEvent integrationEvent)
        where TEvent : class
    {
        await _publishEndpoint.Publish(integrationEvent);

        _logger.LogInformation(
            "{EventType} queued to MassTransit bus outbox. ProductId={ProductId}",
            typeof(TEvent).Name,
            ResolveProductId(integrationEvent));
    }

    private static int ResolveProductId<TEvent>(TEvent integrationEvent)
        where TEvent : class
    {
        return integrationEvent switch
        {
            WishlistItemAddedEvent addedEvent => addedEvent.ProductId,
            WishlistItemRemovedEvent removedEvent => removedEvent.ProductId,
            _ => 0
        };
    }

    private static int NormalizeLimit(int? limit)
    {
        if (!limit.HasValue)
        {
            return DefaultWishlistPageSize;
        }

        return Math.Clamp(limit.Value, 1, MaxWishlistPageSize);
    }

    private static string EncodeCursor(DateTime addedAt, int itemId)
    {
        var payload = $"{addedAt.Ticks}:{itemId}";
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(payload));
    }

    private static (DateTime? AddedAt, int? ItemId) TryDecodeCursor(string? cursor)
    {
        if (string.IsNullOrWhiteSpace(cursor))
        {
            return (null, null);
        }

        try
        {
            var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(cursor));
            var parts = decoded.Split(':', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 2)
            {
                return (null, null);
            }

            if (!long.TryParse(parts[0], out var ticks) || !int.TryParse(parts[1], out var itemId))
            {
                return (null, null);
            }

            return (new DateTime(ticks, DateTimeKind.Utc), itemId);
        }
        catch
        {
            return (null, null);
        }
    }

    private static WishlistBulkAddToCartSkippedItemDto CreateSkippedItem(int productId, string? productName, string reason)
    {
        return new WishlistBulkAddToCartSkippedItemDto
        {
            ProductId = productId,
            ProductName = productName ?? $"Ürün #{productId}",
            Reason = reason
        };
    }

    private static string BuildBulkAddSummaryMessage(WishlistBulkAddToCartResultDto result)
    {
        if (result.RequestedCount == 0)
        {
            return "Favorilerinizde sepete eklenecek ürün bulunamadı.";
        }

        if (result.SkippedCount == 0)
        {
            return $"{result.AddedCount} ürün sepete eklendi.";
        }

        if (result.AddedCount == 0)
        {
            return $"{result.RequestedCount} ürünün hiçbiri sepete eklenemedi.";
        }

        return $"{result.RequestedCount} üründen {result.AddedCount} ürün sepete eklendi, {result.SkippedCount} ürün atlandı.";
    }

    private static WishlistShareSettingsDto CreateShareSettingsDto(Wishlist wishlist)
    {
        return new WishlistShareSettingsDto
        {
            IsPublic = wishlist.IsPublic,
            ShareToken = wishlist.ShareToken,
            SharePath = wishlist.ShareToken.HasValue ? $"/wishlist/share/{wishlist.ShareToken.Value:D}" : null
        };
    }

    private static string ResolveOwnerDisplayName(User? user)
    {
        if (user == null)
        {
            return "Bir kullanıcı";
        }

        var fullName = $"{user.FirstName} {user.LastName}".Trim();
        if (!string.IsNullOrWhiteSpace(fullName))
        {
            return fullName;
        }

        if (!string.IsNullOrWhiteSpace(user.Email))
        {
            var emailPrefix = user.Email.Split('@', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(emailPrefix))
            {
                return emailPrefix;
            }
        }

        return "Bir kullanıcı";
    }
}
