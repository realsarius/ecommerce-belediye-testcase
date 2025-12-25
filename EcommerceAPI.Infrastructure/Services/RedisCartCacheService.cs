using EcommerceAPI.Core.Interfaces;
using EcommerceAPI.Core.Utilities.Redis;
using StackExchange.Redis;

namespace EcommerceAPI.Infrastructure.Services;

/// <summary>
/// Redis tabanlı sepet cache implementasyonu.
/// Redis Hash veri yapısını kullanarak kullanıcı sepetlerini yönetir.
/// 
/// Veri Yapısı:
/// - Key: cart:user:{userId}
/// - Field: productId
/// - Value: quantity
/// 
/// Özellikler:
/// - Atomic işlemler (HashIncrement)
/// - Otomatik expiration (7 gün)
/// - Yüksek performanslı okuma/yazma
/// </summary>
public class RedisCartCacheService : ICartCacheService
{
    private readonly IConnectionMultiplexer _redis;
    private static readonly TimeSpan CartExpiration = TimeSpan.FromDays(Constants.InfrastructureConstants.Redis.CartCacheDays);

    public RedisCartCacheService(IConnectionMultiplexer redis)
    {
        _redis = redis;
    }

    public async Task<Dictionary<int, int>> GetCartItemsAsync(int userId)
    {
        var db = _redis.GetDatabase();
        var cartKey = RedisKeys.Cart(userId);
        
        var entries = await db.HashGetAllAsync(cartKey);
        var result = new Dictionary<int, int>();

        foreach (var entry in entries)
        {
            if (int.TryParse(entry.Name, out int productId) && 
                int.TryParse(entry.Value, out int quantity))
            {
                result[productId] = quantity;
            }
        }

        // Sepet süresini ötele
        if (result.Count > 0)
        {
            await db.KeyExpireAsync(cartKey, CartExpiration);
        }

        return result;
    }

    public async Task IncrementItemQuantityAsync(int userId, int productId, int quantity)
    {
        var db = _redis.GetDatabase();
        var cartKey = RedisKeys.Cart(userId);

        await db.HashIncrementAsync(cartKey, productId, quantity);
        await db.KeyExpireAsync(cartKey, CartExpiration);
    }

    public async Task SetItemQuantityAsync(int userId, int productId, int quantity)
    {
        var db = _redis.GetDatabase();
        var cartKey = RedisKeys.Cart(userId);

        if (quantity <= 0)
        {
            await db.HashDeleteAsync(cartKey, productId);
        }
        else
        {
            await db.HashSetAsync(cartKey, productId, quantity);
        }

        await db.KeyExpireAsync(cartKey, CartExpiration);
    }

    public async Task RemoveItemAsync(int userId, int productId)
    {
        var db = _redis.GetDatabase();
        var cartKey = RedisKeys.Cart(userId);

        await db.HashDeleteAsync(cartKey, productId);
    }

    public async Task ClearCartAsync(int userId)
    {
        var db = _redis.GetDatabase();
        var cartKey = RedisKeys.Cart(userId);

        await db.KeyDeleteAsync(cartKey);
    }

    public async Task<int> GetItemQuantityAsync(int userId, int productId)
    {
        var db = _redis.GetDatabase();
        var cartKey = RedisKeys.Cart(userId);

        var value = await db.HashGetAsync(cartKey, productId);
        
        if (value.HasValue && int.TryParse(value, out int quantity))
        {
            return quantity;
        }

        return 0;
    }

    public async Task<bool> ItemExistsAsync(int userId, int productId)
    {
        var db = _redis.GetDatabase();
        var cartKey = RedisKeys.Cart(userId);

        return await db.HashExistsAsync(cartKey, productId);
    }
}
