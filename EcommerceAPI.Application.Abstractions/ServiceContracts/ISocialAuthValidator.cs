using EcommerceAPI.Entities.DTOs;

namespace EcommerceAPI.Application.Abstractions.ServiceContracts;

public interface ISocialAuthValidator
{
    Task<SocialAuthValidationResult> ValidateAsync(
        string provider,
        string idToken,
        CancellationToken cancellationToken = default);
}
