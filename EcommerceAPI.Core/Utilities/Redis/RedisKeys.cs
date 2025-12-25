namespace EcommerceAPI.Core.Utilities.Redis;

/// <summary>
/// Redis key'lerini merkezi olarak yöneten statik sınıf.
/// Tüm Redis key formatları burada tanımlanır, böylece:
/// - Magic string kullanımı önlenir
/// - Key stratejisi değişikliklerinde tek noktadan güncelleme yapılır
/// - Tutarlı key formatı sağlanır
/// </summary>
public static class RedisKeys
{
    /// <summary>
    /// Kullanıcının sepet verilerini tutan Hash key'i.
    /// Format: cart:user:{userId}
    /// TTL: 7 gün (CartManager tarafından yönetilir)
    /// </summary>
    public static string Cart(int userId) => $"cart:user:{userId}";

    /// <summary>
    /// Ürün stok işlemleri için Distributed Lock key'i.
    /// Format: lock:product:{productId}
    /// TTL: 10 saniye (InventoryManager tarafından yönetilir)
    /// </summary>
    public static string ProductLock(int productId) => $"lock:product:{productId}";

    /// <summary>
    /// Rate limiting için kullanıcı bazlı key.
    /// Format: ratelimit:user:{userId}
    /// </summary>
    public static string RateLimitUser(int userId) => $"ratelimit:user:{userId}";

    /// <summary>
    /// Rate limiting için IP bazlı key.
    /// Format: ratelimit:ip:{ipAddress}
    /// </summary>
    public static string RateLimitIp(string ipAddress) => $"ratelimit:ip:{ipAddress}";

    /// <summary>
    /// Session verilerini tutan key.
    /// Format: session:{sessionId}
    /// </summary>
    public static string Session(string sessionId) => $"session:{sessionId}";

    /// <summary>
    /// Cache'lenmiş ürün detayları için key.
    /// Format: cache:product:{productId}
    /// </summary>
    public static string ProductCache(int productId) => $"cache:product:{productId}";

    /// <summary>
    /// Cache'lenmiş kategori listesi için key.
    /// Format: cache:categories
    /// </summary>
    public static string CategoriesCache() => "cache:categories";

    /// <summary>
    /// Key prefix'leri - toplu silme veya pattern matching için kullanılır.
    /// </summary>
    public static class Prefixes
    {
        public const string Cart = "cart:user:";
        public const string ProductLock = "lock:product:";
        public const string RateLimit = "ratelimit:";
        public const string Session = "session:";
        public const string Cache = "cache:";
    }
}
