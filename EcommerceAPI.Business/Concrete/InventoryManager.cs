using EcommerceAPI.Business.Abstract;
using EcommerceAPI.Entities.Concrete;
using EcommerceAPI.DataAccess.Abstract;
using EcommerceAPI.Core.Utilities.Results;
using EcommerceAPI.Core.Utilities.Redis;
using EcommerceAPI.Core.Interfaces;
using EcommerceAPI.Core.CrossCuttingConcerns.Logging;
using EcommerceAPI.Core.Aspects.Autofac.Logging;
using EcommerceAPI.Business.Constants;
using EcommerceAPI.Entities.IntegrationEvents;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace EcommerceAPI.Business.Concrete;

public class InventoryManager : IInventoryService
{
    private const int WishlistLowStockThreshold = 5;
    private readonly IInventoryDal _inventoryDal;
    private readonly IDistributedLockService _lockService;
    private readonly IAuditService _auditService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<InventoryManager> _logger;

    public InventoryManager(
        IInventoryDal inventoryDal, 
        IDistributedLockService lockService, 
        IAuditService auditService,
        IUnitOfWork unitOfWork,
        IPublishEndpoint publishEndpoint,
        ILogger<InventoryManager> logger)
    {
        _inventoryDal = inventoryDal;
        _lockService = lockService;
        _auditService = auditService;
        _unitOfWork = unitOfWork;
        _publishEndpoint = publishEndpoint;
        _logger = logger;
    }

    [LogAspect]
    public async Task<IResult> DecreaseStockAsync(int productId, int quantity, int userId, string reason)
    {
        return await BulkAdjustStocksAsync(new Dictionary<int, int> { { productId, -quantity } }, userId, reason);
    }

    [LogAspect]
    public async Task<IResult> IncreaseStockAsync(int productId, int quantity, int userId, string reason)
    {
        return await BulkAdjustStocksAsync(new Dictionary<int, int> { { productId, quantity } }, userId, reason);
    }

    [LogAspect]
    public async Task<IResult> ReserveStocksAsync(Dictionary<int, int> productQuantities, int userId, string reason)
    {
        // Reserve = Decrease
        var adjustments = productQuantities.ToDictionary(k => k.Key, v => -v.Value);
        return await BulkAdjustStocksAsync(adjustments, userId, reason);
    }

    [LogAspect]
    public async Task ReleaseStocksAsync(Dictionary<int, int> productQuantities, int userId, string reason)
    {
        // Release = Increase
        var adjustments = productQuantities.ToDictionary(k => k.Key, v => v.Value);
        await BulkAdjustStocksAsync(adjustments, userId, reason);
    }

    [LogAspect]
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
                return new ErrorResult($"{Messages.StockNotFound}: Product {productId}");
            }

            var delta = quantityChanges[productId];
            
            // Race condition korunması: RowVersion ile birlikte çalışır
            var lockKey = RedisKeys.ProductLock(productId);
            var lockResult = await _lockService.ExecuteWithLockAsync<IResult>(lockKey, async () =>
            {
                if (delta < 0 && inventory.QuantityAvailable + delta < 0)
                {
                    return new ErrorResult($"{Messages.StockInsufficient}: {inventory.Product?.Name ?? productId.ToString()}. Mevcut: {inventory.QuantityAvailable}");
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

                if (ShouldPublishLowStockAlert(oldStock, inventory.QuantityAvailable))
                {
                    await PublishLowStockEventAsync(productId, inventory.QuantityAvailable, reason);
                }

                return new SuccessResult();
            });

            if (!lockResult.Success) return lockResult;
        }

        return new SuccessResult();
    }

    private async Task PublishLowStockEventAsync(int productId, int stockQuantity, string reason)
    {
        var integrationEvent = new WishlistProductLowStockEvent
        {
            ProductId = productId,
            StockQuantity = stockQuantity,
            Threshold = WishlistLowStockThreshold,
            Reason = reason
        };

        await _publishEndpoint.Publish(integrationEvent);

        _logger.LogInformation(
            "WishlistProductLowStockEvent queued to MassTransit bus outbox. ProductId={ProductId}, StockQuantity={StockQuantity}, Threshold={Threshold}",
            productId,
            stockQuantity,
            WishlistLowStockThreshold);
    }

    private static bool ShouldPublishLowStockAlert(int oldStock, int newStock)
    {
        return oldStock > WishlistLowStockThreshold &&
               newStock > 0 &&
               newStock <= WishlistLowStockThreshold;
    }
}
