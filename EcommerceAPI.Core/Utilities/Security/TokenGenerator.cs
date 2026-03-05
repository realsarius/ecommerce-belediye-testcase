using System.Security.Cryptography;

namespace EcommerceAPI.Core.Utilities.Security;

public static class TokenGenerator
{
    public static string GenerateUrlSafeToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(bytes)
            .Replace("+", "-")
            .Replace("/", "_")
            .TrimEnd('=');
    }
}
