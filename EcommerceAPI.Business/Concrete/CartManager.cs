using EcommerceAPI.Business.Mappers;
using EcommerceAPI.Business.Abstract;
using EcommerceAPI.Entities.DTOs;
using EcommerceAPI.Entities.Concrete;
using EcommerceAPI.DataAccess.Abstract;
using EcommerceAPI.Core.Interfaces;
using EcommerceAPI.Core.Utilities.Results;
using StackExchange.Redis;

namespace EcommerceAPI.Business.Concrete;

public class CartManager : ICartService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly IProductDal _productDal;
    // CartDal artık kullanılmayabilir ama dependency injection'da sorun olmaması için şimdilik kaldırılabilir 
    // veya DB temizliği için tutulabilir. Biz Redis'e geçiyoruz.
    
    public CartManager(
        IConnectionMultiplexer redis,
        IProductDal productDal)
    {
        _redis = redis;
        _productDal = productDal;
    }

    public async Task<IDataResult<CartDto>> GetCartAsync(int userId)
    {
        var db = _redis.GetDatabase();
        string cartKey = $"cart:user:{userId}";
        
        var cartEntries = await db.HashGetAllAsync(cartKey);
        
        var cartDto = new CartDto
        {
            Id = 0, // Redis tabanlı olduğu için cart ID yok
            Items = new List<CartItemDto>(),
            TotalAmount = 0,
            TotalItems = 0
        };

        if (cartEntries.Length == 0)
        {
            return new SuccessDataResult<CartDto>(cartDto);
        }

        // Product'ları çek
        foreach (var entry in cartEntries)
        {
            if (int.TryParse(entry.Name, out int productId) && int.TryParse(entry.Value, out int quantity))
            {
                var product = await _productDal.GetByIdWithDetailsAsync(productId);
                if (product != null && product.IsActive)
                {
                    var price = product.Price;
                    var itemTotal = price * quantity;
                    
                    cartDto.Items.Add(new CartItemDto
                    {
                        Id = 0, // Item ID yok
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
                     // Ürün artık yoksa veya pasifse sepetten silmeli miyiz?
                     // Şimdilik pas geçiyoruz, bir sonraki işlemde (AddToCart/Remove) düzelebilir
                     // Veya asenkron job temizleyebilir.
                     await db.HashDeleteAsync(cartKey, productId);
                }
            }
        }
        
        // Sepet süresini ötele
        await db.KeyExpireAsync(cartKey, TimeSpan.FromDays(7));

        return new SuccessDataResult<CartDto>(cartDto);
    }

    public async Task<IDataResult<CartDto>> AddToCartAsync(int userId, AddToCartRequest request)
    {
        var product = await _productDal.GetByIdWithDetailsAsync(request.ProductId);
        
        if (product == null || !product.IsActive)
            return new ErrorDataResult<CartDto>("Ürün bulunamadı veya aktif değil");

        var availableStock = product.Inventory?.QuantityAvailable ?? 0;
        
        var db = _redis.GetDatabase();
        string cartKey = $"cart:user:{userId}";

        // Mevcut miktarı al
        var currentQtyRedis = await db.HashGetAsync(cartKey, request.ProductId);
        int currentQty = currentQtyRedis.HasValue ? (int)currentQtyRedis : 0;
        
        var totalRequestedQuantity = request.Quantity + currentQty;
        
        if (totalRequestedQuantity > availableStock)
             return new ErrorDataResult<CartDto>($"Stok yetersiz. Talep edilen: {totalRequestedQuantity}, Mevcut: {availableStock}");

        // Redis'e ekle (Increment atomik, ama üstte check yaptık. Race condition için yine de incr kullanılabilir)
        await db.HashIncrementAsync(cartKey, request.ProductId, request.Quantity);
        
        // Sepetin ömrünü uzat
        await db.KeyExpireAsync(cartKey, TimeSpan.FromDays(7));
        
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

        var db = _redis.GetDatabase();
        string cartKey = $"cart:user:{userId}";

        // Varlığını kontrol et
        if (!await db.HashExistsAsync(cartKey, productId))
             return new ErrorDataResult<CartDto>("Ürün sepette bulunamadı");

        if (request.Quantity <= 0)
        {
             await db.HashDeleteAsync(cartKey, productId);
        }
        else
        {
             await db.HashSetAsync(cartKey, productId, request.Quantity);
        }
        
        await db.KeyExpireAsync(cartKey, TimeSpan.FromDays(7));

        return await GetCartAsync(userId);
    }

    public async Task<IDataResult<CartDto>> RemoveFromCartAsync(int userId, int productId)
    {
        var db = _redis.GetDatabase();
        string cartKey = $"cart:user:{userId}";
        
        if (!await db.HashExistsAsync(cartKey, productId))
              return new ErrorDataResult<CartDto>("Ürün sepette bulunamadı");

        await db.HashDeleteAsync(cartKey, productId);
        
        return await GetCartAsync(userId);
    }

    public async Task<IResult> ClearCartAsync(int userId)
    {
        var db = _redis.GetDatabase();
        string cartKey = $"cart:user:{userId}";
        
        await db.KeyDeleteAsync(cartKey);
        
        return new SuccessResult("Sepet temizlendi.");
    }
}
