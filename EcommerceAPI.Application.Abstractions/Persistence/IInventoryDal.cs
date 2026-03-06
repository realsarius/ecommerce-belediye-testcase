using EcommerceAPI.Core.DataAccess;
using EcommerceAPI.Entities.Concrete;

namespace EcommerceAPI.Application.Abstractions.Persistence;

public interface IInventoryDal : IEntityRepository<Inventory>
{
    Task<Inventory?> GetByProductIdAsync(int productId);
    Task<List<Inventory>> GetByProductIdsAsync(List<int> productIds);
    Task<IList<Inventory>> GetLowStockAsync(int threshold);
    Task AddMovementAsync(InventoryMovement movement);
}
