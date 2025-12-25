using EcommerceAPI.Core.Utilities.Results;

namespace EcommerceAPI.Core.Interfaces;

public interface IDistributedLockService
{
    Task<T> ExecuteWithLockAsync<T>(
        string resourceKey, 
        Func<Task<T>> callback, 
        int lockTimeoutSeconds = 10) where T : IResult;

    Task<string?> TryAcquireLockAsync(string resourceKey, int lockTimeoutSeconds = 10);

    Task ReleaseLockAsync(string resourceKey, string token);
}
