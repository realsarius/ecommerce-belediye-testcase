using EcommerceAPI.Business.Abstract;
using EcommerceAPI.Business.Constants;
using EcommerceAPI.Core.Interfaces;
using EcommerceAPI.Business.Mappers;
using EcommerceAPI.Core.Utilities.Results;
using EcommerceAPI.DataAccess.Abstract;
using EcommerceAPI.Entities.Concrete;
using EcommerceAPI.Entities.DTOs;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace EcommerceAPI.Business.Concrete;

public class WishlistManager : IWishlistService
{
    private readonly IWishlistDal _wishlistDal;
    private readonly IWishlistItemDal _wishlistItemDal;
    private readonly IProductDal _productDal;
    private readonly IWishlistMapper _wishlistMapper;
    private readonly IUnitOfWork _unitOfWork;

    public WishlistManager(
        IWishlistDal wishlistDal, 
        IWishlistItemDal wishlistItemDal, 
        IProductDal productDal, 
        IWishlistMapper wishlistMapper,
        IUnitOfWork unitOfWork)
    {
        _wishlistDal = wishlistDal;
        _wishlistItemDal = wishlistItemDal;
        _productDal = productDal;
        _wishlistMapper = wishlistMapper;
        _unitOfWork = unitOfWork;
    }

    public async Task<IDataResult<WishlistDto>> GetWishlistByUserIdAsync(int userId)
    {
        var wishlist = await _wishlistDal.GetAsync(w => w.UserId == userId);
        if (wishlist == null)
        {
            return new SuccessDataResult<WishlistDto>(new WishlistDto { UserId = userId });
        }

        var items = await _wishlistItemDal.GetListAsync(wi => wi.WishlistId == wishlist.Id);
        if (items != null)
        {
            wishlist.Items = new List<WishlistItem>();
            foreach (var item in items)
            {
                item.Product = await _productDal.GetAsync(p => p.Id == item.ProductId);
                // Only include active products
                if (item.Product != null && item.Product.IsActive)
                {
                    wishlist.Items.Add(item);
                }
            }
        }

        var dto = _wishlistMapper.ToWishlistDto(wishlist);
        return new SuccessDataResult<WishlistDto>(dto);
    }

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

        var wishlist = await _wishlistDal.GetAsync(w => w.UserId == userId);
        if (wishlist == null)
        {
            wishlist = new Wishlist { UserId = userId };
            await _wishlistDal.AddAsync(wishlist);
            await _unitOfWork.SaveChangesAsync();
        }

        // Check wishlist limit (500 items max)
        var itemCount = await _wishlistItemDal.CountAsync(wi => wi.WishlistId == wishlist.Id);

        if (itemCount >= 500)
        {
            return new ErrorResult("Favoriler listesi maksimum kapasiteye ulaştı (500 ürün).");
        }

        var existingItems = await _wishlistItemDal.GetListAsync(wi => wi.WishlistId == wishlist.Id && wi.ProductId == productId);
        if (existingItems != null && existingItems.Any())
        {
            return new SuccessResult("Product is already in wishlist.");
        }

        var newItem = new WishlistItem
        {
            WishlistId = wishlist.Id,
            ProductId = productId,
            AddedAtPrice = product.Price,
            AddedAt = DateTime.UtcNow
        };
        await _wishlistItemDal.AddAsync(newItem);
        await _unitOfWork.SaveChangesAsync();

        return new SuccessResult("Product added to wishlist.");
    }

    public async Task<IResult> RemoveItemFromWishlistAsync(int userId, int productId)
    {
        var wishlist = await _wishlistDal.GetAsync(w => w.UserId == userId);
        if (wishlist == null)
        {
            return new SuccessResult("Item removed."); // idempotent
        }

        var items = await _wishlistItemDal.GetListAsync(wi => wi.WishlistId == wishlist.Id && wi.ProductId == productId);
        var item = items?.FirstOrDefault();
        if (item != null)
        {
            _wishlistItemDal.Delete(item);
            await _unitOfWork.SaveChangesAsync();
        }

        return new SuccessResult("Product removed from wishlist.");
    }

    public async Task<IResult> ClearWishlistAsync(int userId)
    {
        var wishlist = await _wishlistDal.GetAsync(w => w.UserId == userId);
        if (wishlist == null)
        {
            return new SuccessResult("Wishlist is empty.");
        }

        var items = await _wishlistItemDal.GetListAsync(wi => wi.WishlistId == wishlist.Id);
        if (items != null)
        {
            foreach (var item in items)
            {
                _wishlistItemDal.Delete(item);
            }
            await _unitOfWork.SaveChangesAsync();
        }

        return new SuccessResult("Wishlist cleared.");
    }
}
