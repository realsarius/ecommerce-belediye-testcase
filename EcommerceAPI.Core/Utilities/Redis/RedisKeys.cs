namespace EcommerceAPI.Core.Utilities.Redis;

public static class RedisKeys
{
    public static string Cart(int userId) => $"cart:user:{userId}";

    public static string ProductLock(int productId) => $"lock:product:{productId}";

    public static string PaymentLock(int orderId) => $"lock:payment:order:{orderId}";

    public static string RateLimitUser(int userId) => $"ratelimit:user:{userId}";

    public static string RateLimitIp(string ipAddress) => $"ratelimit:ip:{ipAddress}";

    public static string Session(string sessionId) => $"session:{sessionId}";

    public static string ProductCache(int productId) => $"cache:product:{productId}";
    public static string RecommendationRecentlyViewed(string scope) => $"recommendation:recent:{scope}";
    public static string RecommendationAlsoViewed(int productId) => $"recommendation:also-viewed:{productId}";
    public static string RecommendationFrequentlyBought(int productId) => $"recommendation:frequently-bought:{productId}";
    public static string RecommendationProductViews(int productId) => $"recommendation:views:product:{productId}";
    public static string RecommendationProductViewsDaily(int productId, DateOnly date) => $"recommendation:views:daily:product:{productId}:{date:yyyy-MM-dd}";
    public static string RecommendationSearchHistory(int userId) => $"recommendation:search-history:user:{userId}";
    public static string RecommendationWishlistPreferences(int userId) => $"wishlist:preferences:user:{userId}";

    public static string CategoriesCache() => "cache:categories";

    public static class Prefixes
    {
        public const string Cart = "cart:user:";
        public const string ProductLock = "lock:product:";
        public const string PaymentLock = "lock:payment:";
        public const string RateLimit = "ratelimit:";
        public const string Session = "session:";
        public const string Cache = "cache:";
    }
}
