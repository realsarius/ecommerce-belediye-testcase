using System.Text.Json;
using EcommerceAPI.Entities.Concrete;
using EcommerceAPI.DataAccess;
using EcommerceAPI.DataAccess.Concrete.EntityFramework.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using EcommerceAPI.Core.Interfaces;

namespace EcommerceAPI.Seeder;

public class SeedRunner
{
    private readonly AppDbContext _context;
    private readonly ILogger<SeedRunner> _logger;
    private readonly string _seedDataPath;
    private readonly IHashingService _hashingService;

    public SeedRunner(AppDbContext context, ILogger<SeedRunner> logger, string seedDataPath, IHashingService hashingService)
    {
        _context = context;
        _logger = logger;
        _seedDataPath = seedDataPath;
        _hashingService = hashingService;
    }

    public async Task RunAsync(bool reset = false, bool seed = false)
    {
        _logger.LogInformation("🚀 EcommerceAPI Seeder başlatılıyor...");

        if (reset)
        {
            await ResetDatabaseAsync();
        }

        if (seed)
        {
            await SeedAllAsync();
        }

        _logger.LogInformation("✅ Seeder tamamlandı!");
    }

    private async Task ResetDatabaseAsync()
    {
        _logger.LogWarning("🗑️  Veritabanı temizleniyor...");

        // FK sırasına dikkat ederek silme (tersine sıra)
        await _context.Database.ExecuteSqlRawAsync("DELETE FROM \"TBL_InventoryMovements\"");
        await _context.Database.ExecuteSqlRawAsync("DELETE FROM \"TBL_Inventories\"");
        await _context.Database.ExecuteSqlRawAsync("DELETE FROM \"TBL_OrderItems\"");
        await _context.Database.ExecuteSqlRawAsync("DELETE FROM \"TBL_CartItems\"");
        await _context.Database.ExecuteSqlRawAsync("DELETE FROM \"TBL_Products\"");
        await _context.Database.ExecuteSqlRawAsync("DELETE FROM \"TBL_Categories\"");

        _logger.LogInformation("✅ Veritabanı temizlendi.");
    }

    private async Task SeedAllAsync()
    {
        await SeedRolesAsync();
        await SeedUsersAsync();
        await SeedShippingAddressesAsync();
        
        await SeedCategoriesAsync();
        await SeedProductsAsync();
        await SeedInventoriesAsync();

        await ResetSequencesAsync();
    }

    private async Task SeedCategoriesAsync()
    {
        if (await _context.Categories.AnyAsync())
        {
            _logger.LogInformation("📂 Kategoriler zaten mevcut, atlanıyor...");
            return;
        }

        var filePath = Path.Combine(_seedDataPath, "categories.json");
        if (!File.Exists(filePath))
        {
            _logger.LogError("❌ Dosya bulunamadı: {FilePath}", filePath);
            return;
        }

        var json = await File.ReadAllTextAsync(filePath);
        var categoryDtos = JsonSerializer.Deserialize<List<CategorySeedDto>>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        if (categoryDtos == null || categoryDtos.Count == 0)
        {
            _logger.LogWarning("⚠️  Kategori verisi boş.");
            return;
        }

        var categories = categoryDtos.Select(dto => new Category
        {
            Name = dto.Name,
            Description = dto.Description,
            IsActive = dto.IsActive,
            CreatedAt = dto.CreatedAt,
            UpdatedAt = dto.UpdatedAt
        }).ToList();

        // Identity insert için özel SQL kullan
        foreach (var dto in categoryDtos)
        {
            await _context.Database.ExecuteSqlRawAsync(
                @"INSERT INTO ""TBL_Categories"" (""Id"", ""Name"", ""Description"", ""IsActive"", ""CreatedAt"", ""UpdatedAt"") 
                  VALUES ({0}, {1}, {2}, {3}, {4}, {5})",
                dto.Id, dto.Name, dto.Description, dto.IsActive, dto.CreatedAt, dto.UpdatedAt);
        }

        _logger.LogInformation("✅ {Count} kategori eklendi.", categoryDtos.Count);
    }

    private async Task SeedProductsAsync()
    {
        if (await _context.Products.AnyAsync())
        {
            _logger.LogInformation("📦 Ürünler zaten mevcut, atlanıyor...");
            return;
        }

        var filePath = Path.Combine(_seedDataPath, "products.json");
        if (!File.Exists(filePath))
        {
            _logger.LogError("❌ Dosya bulunamadı: {FilePath}", filePath);
            return;
        }

        var json = await File.ReadAllTextAsync(filePath);
        var productDtos = JsonSerializer.Deserialize<List<ProductSeedDto>>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        if (productDtos == null || productDtos.Count == 0)
        {
            _logger.LogWarning("⚠️  Ürün verisi boş.");
            return;
        }

        foreach (var dto in productDtos)
        {
            var sku = $"SKU-{dto.Id:D4}-{Random.Shared.Next(1000, 9999)}";
            await _context.Database.ExecuteSqlRawAsync(
                @"INSERT INTO ""TBL_Products"" (""Id"", ""Name"", ""Description"", ""Price"", ""Currency"", ""SKU"", ""IsActive"", ""CategoryId"", ""CreatedAt"", ""UpdatedAt"") 
                  VALUES ({0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}, {8}, {9})",
                dto.Id, dto.Name, dto.Description, dto.Price, dto.Currency, sku, dto.IsActive, dto.CategoryId, dto.CreatedAt, dto.UpdatedAt);
        }

        _logger.LogInformation("✅ {Count} ürün eklendi.", productDtos.Count);
    }

    private async Task SeedInventoriesAsync()
    {
        if (await _context.Inventories.AnyAsync())
        {
            _logger.LogInformation("📊 Stok verileri zaten mevcut, atlanıyor...");
            return;
        }

        var filePath = Path.Combine(_seedDataPath, "inventories.json");
        if (!File.Exists(filePath))
        {
            _logger.LogError("❌ Dosya bulunamadı: {FilePath}", filePath);
            return;
        }

        var json = await File.ReadAllTextAsync(filePath);
        var inventoryDtos = JsonSerializer.Deserialize<List<InventorySeedDto>>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        if (inventoryDtos == null || inventoryDtos.Count == 0)
        {
            _logger.LogWarning("⚠️  Stok verisi boş.");
            return;
        }

        foreach (var dto in inventoryDtos)
        {
            await _context.Database.ExecuteSqlRawAsync(
                @"INSERT INTO ""TBL_Inventories"" (""ProductId"", ""QuantityAvailable"", ""QuantityReserved"", ""UpdatedAt"") 
                  VALUES ({0}, {1}, {2}, {3})",
                dto.ProductId, dto.QuantityAvailable, dto.QuantityReserved, dto.UpdatedAt);
        }

        _logger.LogInformation("✅ {Count} stok kaydı eklendi.", inventoryDtos.Count);
    }
}

// Seed DTO'ları
public record CategorySeedDto(int Id, string Name, string Description, bool IsActive, DateTime CreatedAt, DateTime UpdatedAt);
public record ProductSeedDto(int Id, string Name, string Description, decimal Price, string Currency, int Sku, bool IsActive, int CategoryId, DateTime CreatedAt, DateTime UpdatedAt);
public record InventorySeedDto(int ProductId, int QuantityAvailable, int QuantityReserved, DateTime UpdatedAt);
