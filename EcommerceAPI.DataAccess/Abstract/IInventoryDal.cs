using EcommerceAPI.Core.DataAccess;
using EcommerceAPI.Entities.Concrete;

namespace EcommerceAPI.DataAccess.Abstract;

public interface IInventoryDal : IEntityRepository<Inventory>
{
    Task<Inventory?> GetByProductIdAsync(int productId);
    Task<IList<Inventory>> GetLowStockAsync(int threshold);
    Task AddMovementAsync(InventoryMovement movement);
}
