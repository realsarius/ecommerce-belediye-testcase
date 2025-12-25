using EcommerceAPI.Business.Abstract;
using EcommerceAPI.Entities.DTOs;
using EcommerceAPI.DataAccess.Abstract;
using EcommerceAPI.Core.Interfaces;
using EcommerceAPI.Core.Utilities.Results;

namespace EcommerceAPI.Business.Concrete;

/// <summary>
/// Cart management service.
/// </summary>
public class CartManager : ICartService
{
    private readonly ICartCacheService _cartCache;
    private readonly IProductDal _productDal;
    
    public CartManager(
        ICartCacheService cartCache,
        IProductDal productDal)
    {
        _cartCache = cartCache;
        _productDal = productDal;
    }

    public async Task<IDataResult<CartDto>> GetCartAsync(int userId)
    {
        var cartItems = await _cartCache.GetCartItemsAsync(userId);
        
        var cartDto = new CartDto
        {
            Id = 0,
            Items = new List<CartItemDto>(),
            TotalAmount = 0,
            TotalItems = 0
        };

        if (cartItems.Count == 0)
        {
            return new SuccessDataResult<CartDto>(cartDto);
        }

        foreach (var (productId, quantity) in cartItems)
        {
            var product = await _productDal.GetByIdWithDetailsAsync(productId);
            if (product != null && product.IsActive)
            {
                var price = product.Price;
                var itemTotal = price * quantity;
                
                cartDto.Items.Add(new CartItemDto
                {
                    Id = 0,
                    ProductId = productId,
                    ProductName = product.Name,
                    ProductSKU = product.SKU,
                    Quantity = quantity,
                    UnitPrice = price,
                    TotalPrice = itemTotal,
                    AvailableStock = product.Inventory?.QuantityAvailable ?? 0
                });

                cartDto.TotalAmount += itemTotal;
                cartDto.TotalItems += quantity;
            }
            else
            {
                await _cartCache.RemoveItemAsync(userId, productId);
            }
        }

        return new SuccessDataResult<CartDto>(cartDto);
    }

    public async Task<IDataResult<CartDto>> AddToCartAsync(int userId, AddToCartRequest request)
    {
        var product = await _productDal.GetByIdWithDetailsAsync(request.ProductId);
        
        if (product == null || !product.IsActive)
            return new ErrorDataResult<CartDto>("Ürün bulunamadı veya aktif değil");

        var availableStock = product.Inventory?.QuantityAvailable ?? 0;
        
        var currentQty = await _cartCache.GetItemQuantityAsync(userId, request.ProductId);
        var totalRequestedQuantity = request.Quantity + currentQty;
        
        if (totalRequestedQuantity > availableStock)
            return new ErrorDataResult<CartDto>($"Stok yetersiz. Talep edilen: {totalRequestedQuantity}, Mevcut: {availableStock}");

        await _cartCache.IncrementItemQuantityAsync(userId, request.ProductId, request.Quantity);
        
        return await GetCartAsync(userId);
    }

    public async Task<IDataResult<CartDto>> UpdateCartItemAsync(int userId, int productId, UpdateCartItemRequest request)
    {
        var product = await _productDal.GetByIdWithDetailsAsync(productId);
        if (product == null || !product.IsActive)
            return new ErrorDataResult<CartDto>("Ürün bulunamadı.");

        var availableStock = product.Inventory?.QuantityAvailable ?? 0;
        
        if (request.Quantity > availableStock)
            return new ErrorDataResult<CartDto>($"Stok yetersiz. Talep edilen: {request.Quantity}, Mevcut: {availableStock}");

        if (!await _cartCache.ItemExistsAsync(userId, productId))
            return new ErrorDataResult<CartDto>("Ürün sepette bulunamadı");

        await _cartCache.SetItemQuantityAsync(userId, productId, request.Quantity);

        return await GetCartAsync(userId);
    }

    public async Task<IDataResult<CartDto>> RemoveFromCartAsync(int userId, int productId)
    {
        if (!await _cartCache.ItemExistsAsync(userId, productId))
            return new ErrorDataResult<CartDto>("Ürün sepette bulunamadı");

        await _cartCache.RemoveItemAsync(userId, productId);
        
        return await GetCartAsync(userId);
    }

    public async Task<IResult> ClearCartAsync(int userId)
    {
        await _cartCache.ClearCartAsync(userId);
        
        return new SuccessResult("Sepet temizlendi.");
    }
}
