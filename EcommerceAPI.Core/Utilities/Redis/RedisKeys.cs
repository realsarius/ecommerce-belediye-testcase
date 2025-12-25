namespace EcommerceAPI.Core.Utilities.Redis;

public static class RedisKeys
{
    public static string Cart(int userId) => $"cart:user:{userId}";

    public static string ProductLock(int productId) => $"lock:product:{productId}";

    public static string RateLimitUser(int userId) => $"ratelimit:user:{userId}";

    public static string RateLimitIp(string ipAddress) => $"ratelimit:ip:{ipAddress}";

    public static string Session(string sessionId) => $"session:{sessionId}";

    public static string ProductCache(int productId) => $"cache:product:{productId}";

    public static string CategoriesCache() => "cache:categories";

    public static class Prefixes
    {
        public const string Cart = "cart:user:";
        public const string ProductLock = "lock:product:";
        public const string RateLimit = "ratelimit:";
        public const string Session = "session:";
        public const string Cache = "cache:";
    }
}
