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
