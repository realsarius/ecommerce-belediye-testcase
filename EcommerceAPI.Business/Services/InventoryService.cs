using EcommerceAPI.Core.Interfaces;

namespace EcommerceAPI.Business.Services;

public class InventoryService : IInventoryService
{
    private readonly IInventoryRepository _inventoryRepository;

    public InventoryService(IInventoryRepository inventoryRepository)
    {
        _inventoryRepository = inventoryRepository;
    }

    public async Task DecreaseStockAsync(int productId, int quantity)
    {
        var inventory = await _inventoryRepository.GetByProductIdAsync(productId);
        if (inventory != null)
        {
            inventory.QuantityAvailable -= quantity;
            _inventoryRepository.Update(inventory);
        }
    }

    public async Task IncreaseStockAsync(int productId, int quantity)
    {
        var inventory = await _inventoryRepository.GetByProductIdAsync(productId);
        if (inventory != null)
        {
            inventory.QuantityAvailable += quantity;
            _inventoryRepository.Update(inventory);
        }
    }
}
