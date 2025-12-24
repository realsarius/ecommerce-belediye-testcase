using EcommerceAPI.Business.Abstract;
using EcommerceAPI.Entities.Concrete;
using EcommerceAPI.DataAccess.Abstract; // IDal interface'leri burada
using EcommerceAPI.Core.Utilities.Results;
using StackExchange.Redis;

namespace EcommerceAPI.Business.Concrete;

public class InventoryManager : IInventoryService
{
    private readonly IInventoryDal _inventoryDal;
    private readonly IConnectionMultiplexer _redis;

    public InventoryManager(IInventoryDal inventoryDal, IConnectionMultiplexer redis)
    {
        _inventoryDal = inventoryDal;
        _redis = redis;
    }

    public async Task<IResult> DecreaseStockAsync(int productId, int quantity, int userId, string reason)
    {
        var db = _redis.GetDatabase();
        var lockKey = $"lock:product:{productId}";
        var token = Guid.NewGuid().ToString();

        // 1. Kilidi almaya çalış (10 saniyelik kilit)
        if (await db.LockTakeAsync(lockKey, token, TimeSpan.FromSeconds(10)))
        {
            try
            {
                // --- KRİTİK BÖLGE BAŞLANGICI ---
                
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
                
                // --- KRİTİK BÖLGE BİTİŞİ ---
                
                return new SuccessResult();
            }
            finally
            {
                // 2. İş bitince kilidi serbest bırak
                await db.LockReleaseAsync(lockKey, token);
            }
        }
        else
        {
            return new ErrorResult("Sistem yoğunluğu nedeniyle işlem gerçekleştirilemedi. Lütfen tekrar deneyin.");
        }
    }

    public async Task<IResult> IncreaseStockAsync(int productId, int quantity, int userId, string reason)
    {
        // IncreaseStock için de lock eklenebilir ama şu an öncelik DecreaseStock (overselling koruması)
        // Tutarlılık için buraya da ekliyoruz.
        var db = _redis.GetDatabase();
        var lockKey = $"lock:product:{productId}";
        var token = Guid.NewGuid().ToString();

        if (await db.LockTakeAsync(lockKey, token, TimeSpan.FromSeconds(10)))
        {
            try 
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
            finally
            {
                 await db.LockReleaseAsync(lockKey, token);
            }
        }
        else 
        {
             return new ErrorResult("Sistem yoğunluğu nedeniyle işlem gerçekleştirilemedi. Lütfen tekrar deneyin.");
        }
    }
}
