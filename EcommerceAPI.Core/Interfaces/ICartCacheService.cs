namespace EcommerceAPI.Core.Interfaces;

public interface ICartCacheService
{
    Task<Dictionary<int, int>> GetCartItemsAsync(int userId);

    Task IncrementItemQuantityAsync(int userId, int productId, int quantity);

    Task SetItemQuantityAsync(int userId, int productId, int quantity);

    Task RemoveItemAsync(int userId, int productId);

    Task ClearCartAsync(int userId);

    Task<int> GetItemQuantityAsync(int userId, int productId);

    Task<bool> ItemExistsAsync(int userId, int productId);
}
