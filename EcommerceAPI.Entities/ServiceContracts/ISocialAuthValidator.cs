using EcommerceAPI.Entities.DTOs;

namespace EcommerceAPI.Business.Abstract;

public interface ISocialAuthValidator
{
    Task<SocialAuthValidationResult> ValidateAsync(
        string provider,
        string idToken,
        CancellationToken cancellationToken = default);
}
