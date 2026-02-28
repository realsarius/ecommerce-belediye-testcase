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

namespace EcommerceAPI.Business.Concrete;

public class WishlistManager : IWishlistService
{
    private readonly IWishlistDal _wishlistDal;
    private readonly IWishlistItemDal _wishlistItemDal;
    private readonly IProductDal _productDal;
    private readonly IWishlistMapper _wishlistMapper;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditService _auditService;

    public WishlistManager(
        IWishlistDal wishlistDal,
        IWishlistItemDal wishlistItemDal,
        IProductDal productDal,
        IWishlistMapper wishlistMapper,
        IUnitOfWork unitOfWork,
        IAuditService auditService)
    {
        _wishlistDal = wishlistDal;
        _wishlistItemDal = wishlistItemDal;
        _productDal = productDal;
        _wishlistMapper = wishlistMapper;
        _unitOfWork = unitOfWork;
        _auditService = auditService;
    }

    [CacheAspect(duration: 10)]
    public async Task<IDataResult<WishlistDto>> GetWishlistByUserIdAsync(int userId)
    {
        var wishlist = await _wishlistDal.GetAsync(w => w.UserId == userId);
        if (wishlist == null)
        {
            return new SuccessDataResult<WishlistDto>(new WishlistDto { UserId = userId });
        }

        var items = await _wishlistItemDal.GetListAsync(wi => wi.WishlistId == wishlist.Id);
        wishlist.Items = new List<WishlistItem>();

        foreach (var item in items)
        {
            item.Product = await _productDal.GetAsync(p => p.Id == item.ProductId);
            wishlist.Items.Add(item);
        }

        var dto = _wishlistMapper.ToWishlistDto(wishlist);
        return new SuccessDataResult<WishlistDto>(dto);
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
        var newItem = new WishlistItem
        {
            WishlistId = wishlist.Id,
            ProductId = productId,
            AddedAtPrice = product.Price,
            AddedAt = now,
            CreatedAt = now,
            UpdatedAt = now
        };

        var wasInserted = await _wishlistItemDal.AddIfNotExistsAsync(newItem);
        if (!wasInserted)
        {
            return new SuccessResult("Product is already in wishlist.");
        }

        await SyncWishlistCountsAsync(productId);
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

        var deletedRowCount = await _wishlistItemDal.DeleteByWishlistAndProductAsync(wishlist.Id, productId);
        if (deletedRowCount > 0)
        {
            await SyncWishlistCountsAsync(productId);
            await _auditService.LogActionAsync(
                userId.ToString(),
                "WishlistItemRemoved",
                "Wishlist",
                new { ProductId = productId, WishlistId = wishlist.Id });
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

        foreach (var item in items)
        {
            _wishlistItemDal.Delete(item);
        }

        if (affectedProductIds.Count > 0)
        {
            await _unitOfWork.SaveChangesAsync();
            await SyncWishlistCountsAsync(affectedProductIds);
            await _auditService.LogActionAsync(
                userId.ToString(),
                "WishlistCleared",
                "Wishlist",
                new { WishlistId = wishlist.Id, ProductIds = affectedProductIds });
        }

        return new SuccessResult("Wishlist cleared.");
    }

    private Task SyncWishlistCountsAsync(params int[] productIds)
    {
        return SyncWishlistCountsAsync((IEnumerable<int>)productIds);
    }

    private async Task SyncWishlistCountsAsync(IEnumerable<int> productIds)
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

        await _unitOfWork.SaveChangesAsync();
    }
}
