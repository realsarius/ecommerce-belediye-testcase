using EcommerceAPI.Core.Entities;
using EcommerceAPI.Core.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EcommerceAPI.Data.Repositories;

public class InventoryRepository : IInventoryRepository
{
    private readonly AppDbContext _context;

    public InventoryRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Inventory?> GetByProductIdAsync(int productId)
    {
        return await _context.Inventories.FirstOrDefaultAsync(i => i.ProductId == productId);
    }

    public async Task<Inventory> AddAsync(Inventory inventory)
    {
        await _context.Inventories.AddAsync(inventory);
        return inventory;
    }

    public void Update(Inventory inventory)
    {
        _context.Inventories.Update(inventory);
    }

    public async Task AddMovementAsync(InventoryMovement movement)
    {
        await _context.InventoryMovements.AddAsync(movement);
    }
}
