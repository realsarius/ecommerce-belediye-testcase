using EcommerceAPI.Business.Abstract;
using EcommerceAPI.Entities.Concrete;
using EcommerceAPI.DataAccess.Abstract;
using EcommerceAPI.Core.Utilities.Results;
using EcommerceAPI.Core.Utilities.Redis;
using EcommerceAPI.Core.Interfaces;
using EcommerceAPI.Core.CrossCuttingConcerns.Logging;

namespace EcommerceAPI.Business.Concrete;

/// <summary>
/// Inventory management service.
/// </summary>
public class InventoryManager : IInventoryService
{
    private readonly IInventoryDal _inventoryDal;
    private readonly IDistributedLockService _lockService;
    private readonly IAuditService _auditService;
    private readonly IUnitOfWork _unitOfWork;

    public InventoryManager(
        IInventoryDal inventoryDal, 
        IDistributedLockService lockService, 
        IAuditService auditService,
        IUnitOfWork unitOfWork)
    {
        _inventoryDal = inventoryDal;
        _lockService = lockService;
        _auditService = auditService;
        _unitOfWork = unitOfWork;
    }

    public async Task<IResult> DecreaseStockAsync(int productId, int quantity, int userId, string reason)
    {
        var lockKey = RedisKeys.ProductLock(productId);

        return await _lockService.ExecuteWithLockAsync<IResult>(lockKey, async () =>
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

            var oldStock = inventory.QuantityAvailable;
            inventory.QuantityAvailable -= quantity;
            _inventoryDal.Update(inventory);

            var movement = new InventoryMovement
            {
                ProductId = productId,
                UserId = userId,
                Delta = -quantity,
                Reason = reason,
                Notes = $"Stok düşüldü. Miktar: {quantity}"
            };
            await _inventoryDal.AddMovementAsync(movement);
            
            await _unitOfWork.SaveChangesAsync();

            await _auditService.LogActionAsync(
                userId.ToString(),
                "DecreaseStock",
                "Inventory",
                new { ProductId = productId, Quantity = quantity, OldStock = oldStock, NewStock = inventory.QuantityAvailable, Reason = reason });

            return new SuccessResult();
        });
    }

    public async Task<IResult> IncreaseStockAsync(int productId, int quantity, int userId, string reason)
    {
        var lockKey = RedisKeys.ProductLock(productId);

        return await _lockService.ExecuteWithLockAsync<IResult>(lockKey, async () =>
        {
            var inventory = await _inventoryDal.GetByProductIdAsync(productId);
            if (inventory == null)
            {
                return new ErrorResult("Stok kaydı bulunamadı.");
            }

            var oldStock = inventory.QuantityAvailable;
            inventory.QuantityAvailable += quantity;
            _inventoryDal.Update(inventory);

            var movement = new InventoryMovement
            {
                ProductId = productId,
                UserId = userId,
                Delta = quantity,
                Reason = reason,
                Notes = $"Stok eklendi. Miktar: {quantity}"
            };
            await _inventoryDal.AddMovementAsync(movement);
            
            await _unitOfWork.SaveChangesAsync();

            await _auditService.LogActionAsync(
                userId.ToString(),
                "IncreaseStock",
                "Inventory",
                new { ProductId = productId, Quantity = quantity, OldStock = oldStock, NewStock = inventory.QuantityAvailable, Reason = reason });

            return new SuccessResult();
        });
    }
}
