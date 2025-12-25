namespace EcommerceAPI.Core.Interfaces;

/// <summary>
/// Sepet verilerini cache üzerinde yönetmek için interface.
/// Business katmanı bu interface'i kullanır, altyapı detaylarını (Redis, Memcached vb.) bilmez.
/// 
/// Avantajları:
/// - Business katmanı cache teknolojisinden bağımsız
/// - Unit test'lerde kolayca mocklanabilir
/// - Farklı cache implementasyonları arasında geçiş yapılabilir
/// </summary>
public interface ICartCacheService
{
    /// <summary>
    /// Kullanıcının sepetindeki tüm ürünleri getirir.
    /// </summary>
    /// <param name="userId">Kullanıcı ID</param>
    /// <returns>ProductId -> Quantity dictionary</returns>
    Task<Dictionary<int, int>> GetCartItemsAsync(int userId);

    /// <summary>
    /// Sepete ürün ekler veya miktarı artırır (atomic increment).
    /// </summary>
    /// <param name="userId">Kullanıcı ID</param>
    /// <param name="productId">Ürün ID</param>
    /// <param name="quantity">Eklenecek miktar</param>
    Task IncrementItemQuantityAsync(int userId, int productId, int quantity);

    /// <summary>
    /// Sepetteki ürünün miktarını belirli bir değere ayarlar.
    /// </summary>
    /// <param name="userId">Kullanıcı ID</param>
    /// <param name="productId">Ürün ID</param>
    /// <param name="quantity">Yeni miktar</param>
    Task SetItemQuantityAsync(int userId, int productId, int quantity);

    /// <summary>
    /// Sepetteki belirli bir ürünü kaldırır.
    /// </summary>
    /// <param name="userId">Kullanıcı ID</param>
    /// <param name="productId">Ürün ID</param>
    Task RemoveItemAsync(int userId, int productId);

    /// <summary>
    /// Sepeti tamamen temizler.
    /// </summary>
    /// <param name="userId">Kullanıcı ID</param>
    Task ClearCartAsync(int userId);

    /// <summary>
    /// Sepetteki ürünün mevcut miktarını getirir.
    /// </summary>
    /// <param name="userId">Kullanıcı ID</param>
    /// <param name="productId">Ürün ID</param>
    /// <returns>Miktar, yoksa 0</returns>
    Task<int> GetItemQuantityAsync(int userId, int productId);

    /// <summary>
    /// Sepette belirli bir ürün olup olmadığını kontrol eder.
    /// </summary>
    /// <param name="userId">Kullanıcı ID</param>
    /// <param name="productId">Ürün ID</param>
    Task<bool> ItemExistsAsync(int userId, int productId);
}
