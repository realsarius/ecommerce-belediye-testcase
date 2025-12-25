using EcommerceAPI.Core.Utilities.Results;

namespace EcommerceAPI.Core.Interfaces;

/// <summary>
/// Distributed Lock (Dağıtık Kilit) servisi için interface.
/// Bu servis, dağıtık sistemlerde eşzamanlılık (concurrency) kontrolü sağlar.
/// 
/// Kullanım Senaryoları:
/// - Stok işlemleri (Overselling önleme)
/// - Ödeme işlemleri
/// - Kritik kaynak erişimi
/// 
/// Avantajları:
/// - Business katmanı altyapı detaylarından (Redis, kilit süresi vb.) izole edilir
/// - Unit test'lerde kolayca mocklanabilir
/// - Farklı lock implementasyonları (Redis, SQL, Memory) arasında geçiş yapılabilir
/// </summary>
public interface IDistributedLockService
{
    /// <summary>
    /// Belirtilen kaynak için kilit alır ve callback fonksiyonunu çalıştırır.
    /// İşlem bitince kilit otomatik olarak serbest bırakılır.
    /// </summary>
    /// <typeparam name="T">Callback'in dönüş tipi</typeparam>
    /// <param name="resourceKey">Kilitlenecek kaynağın benzersiz anahtarı</param>
    /// <param name="callback">Kilit altında çalıştırılacak işlem</param>
    /// <param name="lockTimeoutSeconds">Kilit süresi (varsayılan: 10 saniye)</param>
    /// <returns>Callback'in sonucu veya kilit alınamazsa hata</returns>
    Task<T> ExecuteWithLockAsync<T>(
        string resourceKey, 
        Func<Task<T>> callback, 
        int lockTimeoutSeconds = 10) where T : IResult;

    /// <summary>
    /// Belirtilen kaynak için kilit almayı dener.
    /// </summary>
    /// <param name="resourceKey">Kilitlenecek kaynağın benzersiz anahtarı</param>
    /// <param name="lockTimeoutSeconds">Kilit süresi</param>
    /// <returns>Kilit alındıysa token, alınamadıysa null</returns>
    Task<string?> TryAcquireLockAsync(string resourceKey, int lockTimeoutSeconds = 10);

    /// <summary>
    /// Belirtilen kaynağın kilidini serbest bırakır.
    /// </summary>
    /// <param name="resourceKey">Serbest bırakılacak kaynağın anahtarı</param>
    /// <param name="token">TryAcquireLockAsync'den dönen token</param>
    Task ReleaseLockAsync(string resourceKey, string token);
}
