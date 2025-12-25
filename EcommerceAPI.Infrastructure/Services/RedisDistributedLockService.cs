using EcommerceAPI.Core.Interfaces;
using EcommerceAPI.Core.Utilities.Results;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace EcommerceAPI.Infrastructure.Services;

/// <summary>
/// Redis tabanlı Distributed Lock (Dağıtık Kilit) implementasyonu.
/// StackExchange.Redis'in LockTake/LockRelease mekanizmasını kullanır.
/// 
/// Özellikler:
/// - Atomic lock alma ve bırakma
/// - Token tabanlı güvenlik (yalnızca kilidi alan serbest bırakabilir)
/// - Otomatik timeout ile deadlock önleme
/// - Retry mekanizması (opsiyonel)
/// </summary>
public class RedisDistributedLockService : IDistributedLockService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<RedisDistributedLockService> _logger;
    
    /// <summary>
    /// Varsayılan kilit başarısızlık mesajı
    /// </summary>
    private const string LockFailedMessage = "Sistem yoğunluğu nedeniyle işlem gerçekleştirilemedi. Lütfen tekrar deneyin.";

    public RedisDistributedLockService(
        IConnectionMultiplexer redis,
        ILogger<RedisDistributedLockService> logger)
    {
        _redis = redis;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<T> ExecuteWithLockAsync<T>(
        string resourceKey, 
        Func<Task<T>> callback, 
        int lockTimeoutSeconds = 10) where T : IResult
    {
        var db = _redis.GetDatabase();
        var token = Guid.NewGuid().ToString();
        var lockTimeout = TimeSpan.FromSeconds(lockTimeoutSeconds);

        _logger.LogDebug("Attempting to acquire lock for resource: {ResourceKey}", resourceKey);

        // Kilidi almaya çalış
        if (await db.LockTakeAsync(resourceKey, token, lockTimeout))
        {
            try
            {
                _logger.LogDebug("Lock acquired for resource: {ResourceKey}, Token: {Token}", resourceKey, token);
                
                // Callback'i çalıştır (kritik bölge)
                return await callback();
            }
            finally
            {
                // Kilidi serbest bırak
                await db.LockReleaseAsync(resourceKey, token);
                _logger.LogDebug("Lock released for resource: {ResourceKey}", resourceKey);
            }
        }
        else
        {
            _logger.LogWarning("Failed to acquire lock for resource: {ResourceKey}. System is busy.", resourceKey);
            
            // Generic constraint nedeniyle ErrorResult dönemiyoruz, runtime cast gerekiyor
            // Burada T'nin IResult olduğunu biliyoruz, ama SuccessResult/ErrorResult dönebilmek için
            // caller'ın IResult dönen bir callback sağlaması gerekiyor
            return (T)(IResult)new ErrorResult(LockFailedMessage);
        }
    }

    /// <inheritdoc />
    public async Task<string?> TryAcquireLockAsync(string resourceKey, int lockTimeoutSeconds = 10)
    {
        var db = _redis.GetDatabase();
        var token = Guid.NewGuid().ToString();
        var lockTimeout = TimeSpan.FromSeconds(lockTimeoutSeconds);

        _logger.LogDebug("Attempting to acquire lock for resource: {ResourceKey}", resourceKey);

        if (await db.LockTakeAsync(resourceKey, token, lockTimeout))
        {
            _logger.LogDebug("Lock acquired for resource: {ResourceKey}, Token: {Token}", resourceKey, token);
            return token;
        }

        _logger.LogWarning("Failed to acquire lock for resource: {ResourceKey}", resourceKey);
        return null;
    }

    /// <inheritdoc />
    public async Task ReleaseLockAsync(string resourceKey, string token)
    {
        var db = _redis.GetDatabase();
        await db.LockReleaseAsync(resourceKey, token);
        _logger.LogDebug("Lock released for resource: {ResourceKey}", resourceKey);
    }
}
