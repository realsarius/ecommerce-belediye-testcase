namespace EcommerceAPI.Core.Interfaces;

public interface IAuthRateLimitService
{
    Task<(bool Allowed, int RetryAfterSeconds)> TryConsumeResendVerificationAsync(int userId, CancellationToken cancellationToken = default);
    Task<(bool Allowed, int RetryAfterSeconds)> TryConsumeForgotPasswordAsync(string emailHash, CancellationToken cancellationToken = default);
}
