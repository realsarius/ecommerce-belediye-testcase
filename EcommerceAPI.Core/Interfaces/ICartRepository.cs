using EcommerceAPI.Core.Entities;

namespace EcommerceAPI.Core.Interfaces;

public interface ICartRepository : IRepository<Cart>
{
    Task<Cart?> GetActiveCartByUserIdAsync(int userId);
    Task<Cart?> GetCartWithItemsAsync(int cartId);
    Task<CartItem?> GetCartItemAsync(int cartId, int productId);
    void RemoveCartItem(CartItem cartItem);
}
