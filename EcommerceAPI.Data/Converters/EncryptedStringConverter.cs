using EcommerceAPI.Core.Interfaces;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace EcommerceAPI.Data.Converters;

public class EncryptedStringConverter : ValueConverter<string, string>
{
    public EncryptedStringConverter(IEncryptionService encryptionService)
        : base(
            // Entity -> Database (Encrypt)
            plainText => encryptionService.Encrypt(plainText),
            // Database -> Entity (Decrypt)
            cipherText => encryptionService.Decrypt(cipherText))
    {
    }
}

public class NullableEncryptedStringConverter : ValueConverter<string?, string?>
{
    public NullableEncryptedStringConverter(IEncryptionService encryptionService)
        : base(
            // Entity -> Database (Encrypt)
            plainText => string.IsNullOrEmpty(plainText) ? plainText : encryptionService.Encrypt(plainText),
            // Database -> Entity (Decrypt)
            cipherText => string.IsNullOrEmpty(cipherText) ? cipherText : encryptionService.Decrypt(cipherText))
    {
    }
}
