using EcommerceAPI.Core.DataAccess;
using EcommerceAPI.Entities.Concrete;

namespace EcommerceAPI.Application.Abstractions.Persistence;

public interface ICartDal : IEntityRepository<Cart>
{
    Task<Cart?> GetByUserIdAsync(int userId);
    Task<Cart?> GetByUserIdWithItemsAsync(int userId);
    Task<CartItem?> GetCartItemAsync(int cartId, int productId);
    Task AddCartItemAsync(CartItem item);
}
