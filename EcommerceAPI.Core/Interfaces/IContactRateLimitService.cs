namespace EcommerceAPI.Core.Interfaces;

public interface IContactRateLimitService
{
    Task<(bool Allowed, int RetryAfterSeconds)> TryConsumeAsync(string ipAddress, CancellationToken cancellationToken = default);
}
