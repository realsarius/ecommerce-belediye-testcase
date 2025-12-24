using EcommerceAPI.DataAccess.Concrete.EntityFramework.Contexts;
using EcommerceAPI.DataAccess;
using EcommerceAPI.Seeder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var builder = Host.CreateApplicationBuilder(args);

// Komut satırı argümanlarını parse et
var reset = args.Contains("--reset");
var seed = args.Contains("--seed");

if (!reset && !seed)
{
    Console.WriteLine("Kullanım: dotnet run --project EcommerceAPI.Seeder -- [--reset] [--seed]");
    Console.WriteLine("  --reset  : Tabloları temizler");
    Console.WriteLine("  --seed   : JSON dosyalarından veri yükler");
    Console.WriteLine("  Örnek    : dotnet run --project EcommerceAPI.Seeder -- --reset --seed");
    return;
}

// Connection string (appsettings.json veya environment variable'dan)
var connectionString = Environment.GetEnvironmentVariable("DATABASE_URL") 
    ?? "Host=localhost;Port=5432;Database=ecommerce_dev;Username=ecommerce_dev_user;Password=dev_password";

// DbContext konfigürasyonu
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddTransient<SeedRunner>();

var host = builder.Build();

using var scope = host.Services.CreateScope();
var services = scope.ServiceProvider;
var logger = services.GetRequiredService<ILogger<Program>>();

try
{
    logger.LogInformation("🔌 Veritabanına bağlanılıyor...");
    
    var context = services.GetRequiredService<AppDbContext>();
    
    // Veritabanı bağlantısını test et
    await context.Database.CanConnectAsync();
    logger.LogInformation("✅ Veritabanı bağlantısı başarılı!");

    // Seed data klasörünün yolu
    var seedDataPath = Path.Combine(Directory.GetCurrentDirectory(), "seed-data");
    if (!Directory.Exists(seedDataPath))
    {
        // Proje kök dizininden çalışıyorsa
        seedDataPath = Path.Combine(Directory.GetCurrentDirectory(), "..", "seed-data");
    }
    
    if (!Directory.Exists(seedDataPath))
    {
        logger.LogError("❌ seed-data klasörü bulunamadı!");
        return;
    }

    var runner = new SeedRunner(context, services.GetRequiredService<ILogger<SeedRunner>>(), seedDataPath);
    await runner.RunAsync(reset, seed);
}
catch (Exception ex)
{
    logger.LogError(ex, "❌ Seeder çalışırken hata oluştu!");
}
