using EcommerceAPI.Core.Interfaces;
using EcommerceAPI.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EcommerceAPI.API.Controllers;

/// <summary>
/// Mevcut plain text verileri KVKK uyumlu şifreli formata dönüştürmek için 
/// tek seferlik migration endpoint'leri. Sadece Admin erişimi.
/// </summary>
[ApiController]
[Route("api/v1/admin/data-migration")]
[Authorize(Roles = "Admin")]
public class DataMigrationController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IEncryptionService _encryptionService;
    private readonly ILogger<DataMigrationController> _logger;

    public DataMigrationController(
        AppDbContext context, 
        IEncryptionService encryptionService,
        ILogger<DataMigrationController> logger)
    {
        _context = context;
        _encryptionService = encryptionService;
        _logger = logger;
    }

    /// <summary>
    /// TBL_Orders tablosundaki mevcut plain text ShippingAddress verilerini şifreler.
    /// </summary>
    [HttpPost("encrypt-order-addresses")]
    public async Task<IActionResult> EncryptOrderAddresses()
    {
        try
        {
            _logger.LogInformation("Order ShippingAddress şifreleme migration başlatılıyor...");
            
            // Doğrudan SQL ile okuyarak şifrelenmemiş verileri al
            var orders = await _context.Orders
                .FromSqlRaw("SELECT * FROM \"TBL_Orders\"")
                .AsNoTracking()
                .ToListAsync();

            var encryptedCount = 0;
            var alreadyEncryptedCount = 0;
            var errors = new List<string>();

            foreach (var order in orders)
            {
                try
                {
                    // Şifrelenmemiş mi kontrol et (Base64 formatı olan veriler zaten şifreli)
                    if (string.IsNullOrEmpty(order.ShippingAddress))
                    {
                        continue;
                    }

                    // Zaten şifreli mi kontrol et (Base64 karakterleri ve uzunluk kontrolü)
                    if (IsLikelyEncrypted(order.ShippingAddress))
                    {
                        alreadyEncryptedCount++;
                        continue;
                    }

                    // Plain text veriyi şifrele
                    var encryptedAddress = _encryptionService.Encrypt(order.ShippingAddress);
                    
                    // Doğrudan SQL ile güncelle (EF ValueConverter bypass)
                    await _context.Database.ExecuteSqlRawAsync(
                        "UPDATE \"TBL_Orders\" SET \"ShippingAddress\" = {0} WHERE \"Id\" = {1}",
                        encryptedAddress, order.Id);
                    
                    encryptedCount++;
                    _logger.LogDebug("Order {OrderId} adresi şifrelendi", order.Id);
                }
                catch (Exception ex)
                {
                    errors.Add($"Order {order.Id}: {ex.Message}");
                    _logger.LogError(ex, "Order {OrderId} şifrelenirken hata oluştu", order.Id);
                }
            }

            _logger.LogInformation(
                "Order ShippingAddress migration tamamlandı. Şifrelenen: {Encrypted}, Zaten şifreli: {AlreadyEncrypted}, Hata: {Errors}",
                encryptedCount, alreadyEncryptedCount, errors.Count);

            return Ok(new
            {
                success = true,
                message = "Order adresleri şifreleme migration tamamlandı",
                totalProcessed = orders.Count,
                encrypted = encryptedCount,
                alreadyEncrypted = alreadyEncryptedCount,
                errorCount = errors.Count,
                errors = errors.Take(10) // İlk 10 hatayı göster
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Order ShippingAddress migration sırasında kritik hata");
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Bir string'in muhtemelen şifreli olup olmadığını kontrol eder.
    /// Şifreli veriler Base64 formatında ve belirli uzunlukta olur.
    /// </summary>
    private static bool IsLikelyEncrypted(string value)
    {
        if (string.IsNullOrEmpty(value)) return false;
        
        // Base64 formatı kontrolü
        // Şifreli veri: Nonce (12) + Tag (16) + Cipher = minimum ~40+ byte Base64
        if (value.Length < 40) return false;
        
        // Base64 karakterleri kontrolü
        return value.All(c => 
            char.IsLetterOrDigit(c) || c == '+' || c == '/' || c == '=');
    }

    /// <summary>
    /// Şifrelenmemiş sipariş sayısını kontrol eder.
    /// </summary>
    [HttpGet("check-unencrypted-orders")]
    public async Task<IActionResult> CheckUnencryptedOrders()
    {
        try
        {
            var orders = await _context.Orders
                .FromSqlRaw("SELECT * FROM \"TBL_Orders\"")
                .AsNoTracking()
                .ToListAsync();

            var unencryptedOrders = orders
                .Where(o => !string.IsNullOrEmpty(o.ShippingAddress) && !IsLikelyEncrypted(o.ShippingAddress))
                .Select(o => new { o.Id, o.OrderNumber, AddressPreview = o.ShippingAddress?.Substring(0, Math.Min(30, o.ShippingAddress.Length)) + "..." })
                .ToList();

            return Ok(new
            {
                totalOrders = orders.Count,
                unencryptedCount = unencryptedOrders.Count,
                encryptedCount = orders.Count - unencryptedOrders.Count,
                unencryptedSamples = unencryptedOrders.Take(5)
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }
}
