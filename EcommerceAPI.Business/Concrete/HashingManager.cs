using System.Security.Cryptography;
using System.Text;
using EcommerceAPI.Core.Interfaces;
using Microsoft.Extensions.Configuration;

namespace EcommerceAPI.Business.Concrete;

public class HashingService : IHashingService
{
    private readonly string _pepper;

    public HashingService(IConfiguration configuration)
    {
        _pepper = configuration["HASH_PEPPER"] 
            ?? throw new InvalidOperationException("HASH_PEPPER environment variable is not set. Please set a random pepper string.");
    }

    public string Hash(string input)
    {
        if (string.IsNullOrEmpty(input))
            return string.Empty;


        var combined = _pepper + input;
        var bytes = Encoding.UTF8.GetBytes(combined);
        
        var hashBytes = SHA256.HashData(bytes);
        

        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }

    public bool Verify(string input, string hash)
    {
        if (string.IsNullOrEmpty(input) || string.IsNullOrEmpty(hash))
            return false;

        var computedHash = Hash(input);
        
        // Timing attack'lardan korunmak için sabit zamanlı karşılaştırma
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(computedHash),
            Encoding.UTF8.GetBytes(hash)
        );
    }
}
