using EcommerceAPI.Core.Entities;

namespace EcommerceAPI.Core.Interfaces;

public interface IInventoryRepository
{
    Task<Inventory?> GetByProductIdAsync(int productId);
    Task<Inventory> AddAsync(Inventory inventory);
    void Update(Inventory inventory);
    Task AddMovementAsync(InventoryMovement movement);
}
