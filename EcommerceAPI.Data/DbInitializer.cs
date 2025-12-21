using EcommerceAPI.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EcommerceAPI.Data;

public static class DbInitializer
{
    public static async Task InitializeAsync(AppDbContext context, ILogger logger)
    {
        try
        {
            await context.Database.MigrateAsync();
            
            if (await context.Roles.AnyAsync())
            {
                logger.LogInformation("Database already seeded. Skipping initial data load.");
                return;
            }
            
            logger.LogInformation("Starting database seeding process...");
            
            var roles = new List<Role>
            {
                new Role { Id = 1, Name = "Admin", Description = "Sistem yöneticisi - Tüm yetkilere sahip" },
                new Role { Id = 2, Name = "Customer", Description = "Mağaza müşterisi - Alışveriş yapabilir" },
                new Role { Id = 3, Name = "Seller", Description = "Satıcı - Ürün satışı yapabilir" }
            };
            
            await context.Roles.AddRangeAsync(roles);
            await context.SaveChangesAsync();
            
            // users
            // Password: Test123! (BCrypt hash)
            var passwordHash = "$2a$11$rBNlWqg/4V2Jw!YfG8dfU.K8dGcjlKQMZ3m0H7kXPnHWvnBN6jQFW";
            // RoleId = 1 Admin, 2 Seller, 3 Customer

            var users = new List<User>
            {
                // 1 Admin
                new User
                {
                    Id = 1,
                    FirstName = "Admin",
                    LastName = "User",
                    Email = "admin@ecommerce.com",
                    PasswordHash = passwordHash,
                    RoleId = 1, // Admin
                    CreatedAt = DateTime.UtcNow
                },
                // 2 Sellers
                new User
                {
                    Id = 2,
                    FirstName = "Ahmet",
                    LastName = "Yılmaz",
                    Email = "ahmet.seller@ecommerce.com",
                    PasswordHash = passwordHash,
                    RoleId = 3, // Seller
                    CreatedAt = DateTime.UtcNow
                },
                new User
                {
                    Id = 3,
                    FirstName = "Fatma",
                    LastName = "Kaya",
                    Email = "fatma.seller@ecommerce.com",
                    PasswordHash = passwordHash,
                    RoleId = 3,
                    CreatedAt = DateTime.UtcNow
                },
                // 10 Customers
                new User
                {
                    Id = 4,
                    FirstName = "Mehmet",
                    LastName = "Demir",
                    Email = "mehmet@gmail.com",
                    PasswordHash = passwordHash,
                    RoleId = 2, // Customer
                    CreatedAt = DateTime.UtcNow
                },
                new User
                {
                    Id = 5,
                    FirstName = "Ayşe",
                    LastName = "Çelik",
                    Email = "ayse@gmail.com",
                    PasswordHash = passwordHash,
                    RoleId = 2,
                    CreatedAt = DateTime.UtcNow
                },
                new User
                {
                    Id = 6,
                    FirstName = "Mustafa",
                    LastName = "Şahin",
                    Email = "mustafa@gmail.com",
                    PasswordHash = passwordHash,
                    RoleId = 2,
                    CreatedAt = DateTime.UtcNow
                },
                new User
                {
                    Id = 7,
                    FirstName = "Zeynep",
                    LastName = "Yıldız",
                    Email = "zeynep@gmail.com",
                    PasswordHash = passwordHash,
                    RoleId = 2,
                    CreatedAt = DateTime.UtcNow
                },
                new User
                {
                    Id = 8,
                    FirstName = "Ali",
                    LastName = "Öztürk",
                    Email = "ali@gmail.com",
                    PasswordHash = passwordHash,
                    RoleId = 2,
                    CreatedAt = DateTime.UtcNow
                },
                new User
                {
                    Id = 9,
                    FirstName = "Elif",
                    LastName = "Arslan",
                    Email = "elif@gmail.com",
                    PasswordHash = passwordHash,
                    RoleId = 2,
                    CreatedAt = DateTime.UtcNow
                },
                new User
                {
                    Id = 10,
                    FirstName = "Hasan",
                    LastName = "Koç",
                    Email = "hasan@gmail.com",
                    PasswordHash = passwordHash,
                    RoleId = 2,
                    CreatedAt = DateTime.UtcNow
                },
                new User
                {
                    Id = 11,
                    FirstName = "Merve",
                    LastName = "Aydın",
                    Email = "merve@gmail.com",
                    PasswordHash = passwordHash,
                    RoleId = 2,
                    CreatedAt = DateTime.UtcNow
                },
                new User
                {
                    Id = 12,
                    FirstName = "Emre",
                    LastName = "Çetin",
                    Email = "emre@gmail.com",
                    PasswordHash = passwordHash,
                    RoleId = 2,
                    CreatedAt = DateTime.UtcNow
                },
                new User
                {
                    Id = 13,
                    FirstName = "Selin",
                    LastName = "Korkmaz",
                    Email = "selin@gmail.com",
                    PasswordHash = passwordHash,
                    RoleId = 2,
                    CreatedAt = DateTime.UtcNow
                }
            };
            
            await context.Users.AddRangeAsync(users);
            await context.SaveChangesAsync();
            
            // shipping addresses
            var addresses = new List<ShippingAddress>
            {
                // Mehmet (Id=4) has 2 addresses
                new ShippingAddress
                {
                    Id = 1,
                    UserId = 4,
                    Title = "Ev",
                    FullName = "Mehmet Demir",
                    Phone = "05321234567",
                    City = "İstanbul",
                    District = "Kadıköy",
                    AddressLine = "Caferağa Mah. Moda Cad. No:15 D:3",
                    PostalCode = "34710",
                    IsDefault = true,
                    CreatedAt = DateTime.UtcNow
                },
                new ShippingAddress
                {
                    Id = 2,
                    UserId = 4,
                    Title = "İş",
                    FullName = "Mehmet Demir",
                    Phone = "05321234567",
                    City = "İstanbul",
                    District = "Şişli",
                    AddressLine = "Mecidiyeköy Mah. Büyükdere Cad. No:100 Kat:5",
                    PostalCode = "34394",
                    IsDefault = false,
                    CreatedAt = DateTime.UtcNow
                },
                // Ayşe (Id=5) has 1 address
                new ShippingAddress
                {
                    Id = 3,
                    UserId = 5,
                    Title = "Ev",
                    FullName = "Ayşe Çelik",
                    Phone = "05339876543",
                    City = "Ankara",
                    District = "Çankaya",
                    AddressLine = "Kızılay Mah. Atatürk Bulvarı No:50 D:8",
                    PostalCode = "06420",
                    IsDefault = true,
                    CreatedAt = DateTime.UtcNow
                }
            };
            
            await context.ShippingAddresses.AddRangeAsync(addresses);
            await context.SaveChangesAsync();
            
            var categories = new List<Category>
            {
                new Category { Id = 1, Name = "Elektronik", IsActive = true },
                new Category { Id = 2, Name = "Giyim ve Tekstil", IsActive = true },
                new Category { Id = 3, Name = "Ev ve Yaşam", IsActive = true },
                new Category { Id = 4, Name = "Kitap ve Kırtasiye", IsActive = true },
                new Category { Id = 5, Name = "Spor ve Outdoor", IsActive = true }
            };
            
            await context.Categories.AddRangeAsync(categories);
            await context.SaveChangesAsync();
            
            var products = new List<Product>
            {
                new Product
                {
                    Id = 1,
                    Name = "Logitech G502 Hero Mouse",
                    Description = "Yüksek performanslı kablolu oyuncu mouse, 25K DPI sensör, RGB aydınlatma.",
                    Price = 1850.50m,
                    Currency = "TRY",
                    SKU = "LOG-G502-H",
                    CategoryId = 1,
                    IsActive = true
                },
                new Product
                {
                    Id = 2,
                    Name = "Asus VG248QG Monitör",
                    Description = "24 inç, 165Hz, 0.5ms tepki süresi, G-Sync uyumlu oyuncu monitörü.",
                    Price = 4199.00m,
                    Currency = "TRY",
                    SKU = "ASU-VG24-165",
                    CategoryId = 1,
                    IsActive = true
                },
                new Product
                {
                    Id = 3,
                    Name = "Sony WH-1000XM5 Kulaklık",
                    Description = "Kablosuz gürültü engelleyici kulaklık, Platin Gümüş rengi.",
                    Price = 12499.90m,
                    Currency = "TRY",
                    SKU = "SNY-XM5-SLV",
                    CategoryId = 1,
                    IsActive = true
                },
                new Product
                {
                    Id = 4,
                    Name = "Oversize Pamuklu Tişört",
                    Description = "%100 Organik pamuk, Füme, Uniseks kesim.",
                    Price = 349.90m,
                    Currency = "TRY",
                    SKU = "APP-TSH-CHR-01",
                    CategoryId = 2,
                    IsActive = true
                },
                new Product
                {
                    Id = 5,
                    Name = "Levi's 511 Slim Fit Jean",
                    Description = "Klasik dar kesim denim pantolon, Koyu İndigo.",
                    Price = 1250.00m,
                    Currency = "TRY",
                    SKU = "LEV-511-IND",
                    CategoryId = 2,
                    IsActive = true
                },
                new Product
                {
                    Id = 6,
                    Name = "Stanley Klasik Termos 1.0L",
                    Description = "Vakum yalıtımlı paslanmaz çelik, Hammertone Yeşil.",
                    Price = 1650.00m,
                    Currency = "TRY",
                    SKU = "STN-CLSC-1L-GRN",
                    CategoryId = 5,
                    IsActive = true
                },
                new Product
                {
                    Id = 7,
                    Name = "El Yapımı Seramik Kupa",
                    Description = "Mat yüzey, 350ml kapasite, bulaşık makinesinde yıkanabilir.",
                    Price = 225.00m,
                    Currency = "TRY",
                    SKU = "HOM-MUG-CER-04",
                    CategoryId = 3,
                    IsActive = true
                },
                new Product
                {
                    Id = 8,
                    Name = "Lamy Safari Dolma Kalem",
                    Description = "Mat Siyah gövde, medium uç, mürekkep kartuşu dahil.",
                    Price = 750.00m,
                    Currency = "TRY",
                    SKU = "STA-LAMY-SAF-BLK",
                    CategoryId = 4,
                    IsActive = true
                },
                new Product
                {
                    Id = 9,
                    Name = "Sapiens - Yuval Noah Harari",
                    Description = "Hayvanlardan Tanrılara: İnsan Türünün Kısa Bir Tarihi, Karton kapak.",
                    Price = 145.50m,
                    Currency = "TRY",
                    SKU = "BKS-SAPIENS-TR",
                    CategoryId = 4,
                    IsActive = true
                },
                new Product
                {
                    Id = 10,
                    Name = "North Face Borealis Sırt Çantası",
                    Description = "28L kapasite, laptop bölmesi, Siyah renk.",
                    Price = 2850.00m,
                    Currency = "TRY",
                    SKU = "TNF-BOR-BLK-28",
                    CategoryId = 5,
                    IsActive = true
                }
            };
            
            await context.Products.AddRangeAsync(products);
            await context.SaveChangesAsync();
            
            var inventories = new List<Inventory>
            {
                new Inventory { ProductId = 1, QuantityAvailable = 34, QuantityReserved = 0 },
                new Inventory { ProductId = 2, QuantityAvailable = 12, QuantityReserved = 0 },
                new Inventory { ProductId = 3, QuantityAvailable = 7, QuantityReserved = 0 },
                new Inventory { ProductId = 4, QuantityAvailable = 142, QuantityReserved = 0 },
                new Inventory { ProductId = 5, QuantityAvailable = 56, QuantityReserved = 0 },
                new Inventory { ProductId = 6, QuantityAvailable = 19, QuantityReserved = 0 },
                new Inventory { ProductId = 7, QuantityAvailable = 41, QuantityReserved = 0 },
                new Inventory { ProductId = 8, QuantityAvailable = 23, QuantityReserved = 0 },
                new Inventory { ProductId = 9, QuantityAvailable = 312, QuantityReserved = 0 },
                new Inventory { ProductId = 10, QuantityAvailable = 14, QuantityReserved = 0 }
            };
            
            await context.Inventories.AddRangeAsync(inventories);
            await context.SaveChangesAsync();
            
            logger.LogInformation("Database seeding completed successfully.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred during the database seeding process.");
            throw;
        }
    }
}