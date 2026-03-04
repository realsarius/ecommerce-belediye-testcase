using EcommerceAPI.Core.Interfaces;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace EcommerceAPI.DataAccess.Converters;

public class EncryptedStringConverter : ValueConverter<string, string>
{
    public EncryptedStringConverter(IEncryptionService encryptionService)
        : base(

            plainText => encryptionService.Encrypt(plainText),

            cipherText => encryptionService.Decrypt(cipherText))
    {
    }
}

public class NullableEncryptedStringConverter : ValueConverter<string?, string?>
{
    public NullableEncryptedStringConverter(IEncryptionService encryptionService)
        : base(

            plainText => string.IsNullOrEmpty(plainText) ? plainText : encryptionService.Encrypt(plainText),

            cipherText => string.IsNullOrEmpty(cipherText) ? cipherText : encryptionService.Decrypt(cipherText))
    {
    }
}

public class ProviderTokenStringConverter : ValueConverter<string?, string?>
{
    private const string Prefix = "enc::";

    public ProviderTokenStringConverter(IEncryptionService encryptionService)
        : base(
            plainText => ProtectToken(plainText, encryptionService),
            cipherText => UnprotectToken(cipherText, encryptionService))
    {
    }

    private static string? ProtectToken(string? plainText, IEncryptionService encryptionService)
    {
        if (string.IsNullOrEmpty(plainText))
        {
            return plainText;
        }

        if (plainText.StartsWith(Prefix, StringComparison.Ordinal))
        {
            return plainText;
        }

        return $"{Prefix}{encryptionService.Encrypt(plainText)}";
    }

    private static string? UnprotectToken(string? cipherText, IEncryptionService encryptionService)
    {
        if (string.IsNullOrEmpty(cipherText))
        {
            return cipherText;
        }

        if (!cipherText.StartsWith(Prefix, StringComparison.Ordinal))
        {
            return cipherText;
        }

        return encryptionService.Decrypt(cipherText.Substring(Prefix.Length));
    }
}
