using EcommerceAPI.Business.Abstract;
using EcommerceAPI.Entities.Concrete;
using EcommerceAPI.DataAccess.Abstract; // IDal interface'leri burada
using EcommerceAPI.Core.Utilities.Results;

namespace EcommerceAPI.Business.Concrete;

public class InventoryManager : IInventoryService
{
    private readonly IInventoryDal _inventoryDal;

    public InventoryManager(IInventoryDal inventoryDal)
    {
        _inventoryDal = inventoryDal;
    }

    public async Task<IResult> DecreaseStockAsync(int productId, int quantity, int userId, string reason)
    {
        var inventory = await _inventoryDal.GetByProductIdAsync(productId);
        if (inventory == null)
        {
            return new ErrorResult("Stok kaydı bulunamadı.");
        }

        if (inventory.QuantityAvailable < quantity)
        {
            return new ErrorResult($"Stok yetersiz. Mevcut: {inventory.QuantityAvailable}, İstenen: {quantity}");
        }

        inventory.QuantityAvailable -= quantity;
        _inventoryDal.Update(inventory);

        // Audit kaydı
        var movement = new InventoryMovement
        {
            ProductId = productId,
            UserId = userId,
            Delta = -quantity,
            Reason = reason,
            Notes = $"Stok düşüldü. Miktar: {quantity}"
        };
        await _inventoryDal.AddMovementAsync(movement);
        
        return new SuccessResult();
    }

    public async Task<IResult> IncreaseStockAsync(int productId, int quantity, int userId, string reason)
    {
        var inventory = await _inventoryDal.GetByProductIdAsync(productId);
        if (inventory == null)
        {
            return new ErrorResult("Stok kaydı bulunamadı.");
        }

        inventory.QuantityAvailable += quantity;
        _inventoryDal.Update(inventory);

        // Audit kaydı burada
        var movement = new InventoryMovement
        {
            ProductId = productId,
            UserId = userId,
            Delta = quantity,
            Reason = reason,
            Notes = $"Stok eklendi. Miktar: {quantity}"
        };
        await _inventoryDal.AddMovementAsync(movement);

        return new SuccessResult();
    }
}
