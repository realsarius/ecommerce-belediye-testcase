using EcommerceAPI.Business.Abstract;
using EcommerceAPI.Entities.Concrete;
using EcommerceAPI.DataAccess.Abstract;
using EcommerceAPI.Core.Utilities.Results;
using EcommerceAPI.Core.Utilities.Redis;
using EcommerceAPI.Core.Interfaces;
using EcommerceAPI.Core.CrossCuttingConcerns.Logging;

namespace EcommerceAPI.Business.Concrete;

/// <summary>
/// Stok yönetimi. SaveChangesAsync çağırmaz; çağıran (OrderManager) transaction commit yapar.
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
        return await BulkAdjustStocksAsync(new Dictionary<int, int> { { productId, -quantity } }, userId, reason);
    }

    public async Task<IResult> IncreaseStockAsync(int productId, int quantity, int userId, string reason)
    {
        return await BulkAdjustStocksAsync(new Dictionary<int, int> { { productId, quantity } }, userId, reason);
    }

    public async Task<IResult> ReserveStocksAsync(Dictionary<int, int> productQuantities, int userId, string reason)
    {
        // Reserve = Decrease
        var adjustments = productQuantities.ToDictionary(k => k.Key, v => -v.Value);
        return await BulkAdjustStocksAsync(adjustments, userId, reason);
    }

    public async Task ReleaseStocksAsync(Dictionary<int, int> productQuantities, int userId, string reason)
    {
        // Release = Increase
        var adjustments = productQuantities.ToDictionary(k => k.Key, v => v.Value);
        await BulkAdjustStocksAsync(adjustments, userId, reason);
    }

    public async Task<IResult> BulkAdjustStocksAsync(Dictionary<int, int> quantityChanges, int userId, string reason)
    {
        var productIds = quantityChanges.Keys.ToList();
        
        var inventories = await _inventoryDal.GetByProductIdsAsync(productIds);
        var inventoryMap = inventories.ToDictionary(i => i.ProductId, i => i);

        // Deadlock önleme: key sıralaması
        var sortedKeys = quantityChanges.Keys.OrderBy(k => k).ToList();

        foreach (var productId in sortedKeys)
        {
            if (!inventoryMap.TryGetValue(productId, out var inventory))
            {
                return new ErrorResult($"Stok kaydı bulunamadı: Product {productId}");
            }

            var delta = quantityChanges[productId];
            
            // Race condition korunması: RowVersion ile birlikte çalışır
            var lockKey = RedisKeys.ProductLock(productId);
            var lockResult = await _lockService.ExecuteWithLockAsync<IResult>(lockKey, async () =>
            {
                if (delta < 0 && inventory.QuantityAvailable + delta < 0)
                {
                    return new ErrorResult($"Stok yetersiz: {inventory.Product?.Name ?? productId.ToString()}. Mevcut: {inventory.QuantityAvailable}");
                }

                var oldStock = inventory.QuantityAvailable;
                inventory.QuantityAvailable += delta;
                _inventoryDal.Update(inventory);

                var movement = new InventoryMovement
                {
                    ProductId = productId,
                    UserId = userId,
                    Delta = delta,
                    Reason = reason,
                    Notes = delta < 0 ? $"Stok düşüldü (Bulk). Miktar: {-delta}" : $"Stok eklendi (Bulk). Miktar: {delta}"
                };
                await _inventoryDal.AddMovementAsync(movement);

                return new SuccessResult();
            });

            if (!lockResult.Success) return lockResult;
        }

        return new SuccessResult();
    }
}
