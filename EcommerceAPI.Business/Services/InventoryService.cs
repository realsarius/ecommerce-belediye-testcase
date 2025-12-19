using EcommerceAPI.Core.Entities;
using EcommerceAPI.Core.Interfaces;

namespace EcommerceAPI.Business.Services;

public class InventoryService : IInventoryService
{
    private readonly IInventoryRepository _inventoryRepository;

    public InventoryService(IInventoryRepository inventoryRepository)
    {
        _inventoryRepository = inventoryRepository;
    }

    public async Task DecreaseStockAsync(int productId, int quantity, int userId, string reason)
    {
        var inventory = await _inventoryRepository.GetByProductIdAsync(productId);
        if (inventory != null)
        {
            inventory.QuantityAvailable -= quantity;
            _inventoryRepository.Update(inventory);

            // Audit kaydı
            var movement = new InventoryMovement
            {
                ProductId = productId,
                UserId = userId,
                Delta = -quantity,
                Reason = reason,
                Notes = $"Stok düşüldü. Miktar: {quantity}"
            };
            await _inventoryRepository.AddMovementAsync(movement);
        }
    }

    public async Task IncreaseStockAsync(int productId, int quantity, int userId, string reason)
    {
        var inventory = await _inventoryRepository.GetByProductIdAsync(productId);
        if (inventory != null)
        {
            inventory.QuantityAvailable += quantity;
            _inventoryRepository.Update(inventory);

            // Audit kaydı burada
            var movement = new InventoryMovement
            {
                ProductId = productId,
                UserId = userId,
                Delta = quantity,
                Reason = reason,
                Notes = $"Stok eklendi. Miktar: {quantity}"
            };
            await _inventoryRepository.AddMovementAsync(movement);
        }
    }
}
