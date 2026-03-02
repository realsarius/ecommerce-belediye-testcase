using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Google.Apis.Auth;
using EcommerceAPI.Business.Abstract;
using EcommerceAPI.Entities.DTOs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace EcommerceAPI.Infrastructure.Services;

public class SocialAuthValidator : ISocialAuthValidator
{
    private static readonly SemaphoreSlim AppleKeysLock = new(1, 1);
    private static IReadOnlyCollection<SecurityKey>? _appleKeysCache;
    private static DateTimeOffset _appleKeysExpiresAt = DateTimeOffset.MinValue;

    private readonly IConfiguration _configuration;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<SocialAuthValidator> _logger;

    public SocialAuthValidator(
        IConfiguration configuration,
        IHttpClientFactory httpClientFactory,
        ILogger<SocialAuthValidator> logger)
    {
        _configuration = configuration;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<SocialAuthValidationResult> ValidateAsync(
        string provider,
        string idToken,
        CancellationToken cancellationToken = default)
    {
        return provider.Trim().ToLowerInvariant() switch
        {
            "google" => await ValidateGoogleAsync(idToken),
            "apple" => await ValidateAppleAsync(idToken, cancellationToken),
            _ => new SocialAuthValidationResult
            {
                Success = false,
                ErrorMessage = "Desteklenmeyen sosyal giriş sağlayıcısı."
            }
        };
    }

    private async Task<SocialAuthValidationResult> ValidateGoogleAsync(string idToken)
    {
        var googleClientId = _configuration["GOOGLE_CLIENT_ID"]
                             ?? _configuration["SocialAuth:GoogleClientId"];

        if (string.IsNullOrWhiteSpace(googleClientId))
        {
            return new SocialAuthValidationResult
            {
                Success = false,
                ErrorMessage = "Google sosyal giriş yapılandırılmamış."
            };
        }

        try
        {
            var payload = await GoogleJsonWebSignature.ValidateAsync(
                idToken,
                new GoogleJsonWebSignature.ValidationSettings
                {
                    Audience = [googleClientId]
                });

            if (string.IsNullOrWhiteSpace(payload.Email))
            {
                return new SocialAuthValidationResult
                {
                    Success = false,
                    ErrorMessage = "Google hesabından doğrulanmış e-posta alınamadı."
                };
            }

            return new SocialAuthValidationResult
            {
                Success = true,
                Provider = "google",
                Subject = payload.Subject,
                Email = payload.Email,
                EmailVerified = payload.EmailVerified,
                FirstName = payload.GivenName,
                LastName = payload.FamilyName
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Google sosyal giriş tokenı doğrulanamadı.");
            return new SocialAuthValidationResult
            {
                Success = false,
                ErrorMessage = "Google kimlik doğrulaması başarısız oldu."
            };
        }
    }

    private async Task<SocialAuthValidationResult> ValidateAppleAsync(string idToken, CancellationToken cancellationToken)
    {
        var appleClientId = _configuration["APPLE_CLIENT_ID"]
                            ?? _configuration["SocialAuth:AppleClientId"];

        if (string.IsNullOrWhiteSpace(appleClientId))
        {
            return new SocialAuthValidationResult
            {
                Success = false,
                ErrorMessage = "Apple sosyal giriş yapılandırılmamış."
            };
        }

        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var principal = tokenHandler.ValidateToken(
                idToken,
                new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = "https://appleid.apple.com",
                    ValidateAudience = true,
                    ValidAudience = appleClientId,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKeys = await GetAppleSigningKeysAsync(cancellationToken),
                    ClockSkew = TimeSpan.FromMinutes(1)
                },
                out var validatedToken);

            var jwt = validatedToken as JwtSecurityToken;
            var email = principal.FindFirstValue("email");
            var emailVerifiedRaw = principal.FindFirstValue("email_verified");

            if (string.IsNullOrWhiteSpace(email) || jwt == null)
            {
                return new SocialAuthValidationResult
                {
                    Success = false,
                    ErrorMessage = "Apple hesabından doğrulanmış e-posta alınamadı."
                };
            }

            return new SocialAuthValidationResult
            {
                Success = true,
                Provider = "apple",
                Subject = principal.FindFirstValue(ClaimTypes.NameIdentifier)
                          ?? principal.FindFirstValue("sub")
                          ?? string.Empty,
                Email = email,
                EmailVerified = string.Equals(emailVerifiedRaw, "true", StringComparison.OrdinalIgnoreCase),
                FirstName = principal.FindFirstValue(ClaimTypes.GivenName),
                LastName = principal.FindFirstValue(ClaimTypes.Surname)
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Apple sosyal giriş tokenı doğrulanamadı.");
            return new SocialAuthValidationResult
            {
                Success = false,
                ErrorMessage = "Apple kimlik doğrulaması başarısız oldu."
            };
        }
    }

    private async Task<IReadOnlyCollection<SecurityKey>> GetAppleSigningKeysAsync(CancellationToken cancellationToken)
    {
        if (_appleKeysCache != null && _appleKeysExpiresAt > DateTimeOffset.UtcNow)
        {
            return _appleKeysCache;
        }

        await AppleKeysLock.WaitAsync(cancellationToken);
        try
        {
            if (_appleKeysCache != null && _appleKeysExpiresAt > DateTimeOffset.UtcNow)
            {
                return _appleKeysCache;
            }

            var client = _httpClientFactory.CreateClient();
            var jwksJson = await client.GetStringAsync("https://appleid.apple.com/auth/keys", cancellationToken);
            var jwks = new JsonWebKeySet(jwksJson);
            _appleKeysCache = jwks.Keys.Cast<SecurityKey>().ToList();
            _appleKeysExpiresAt = DateTimeOffset.UtcNow.AddHours(6);
            return _appleKeysCache;
        }
        finally
        {
            AppleKeysLock.Release();
        }
    }
}
