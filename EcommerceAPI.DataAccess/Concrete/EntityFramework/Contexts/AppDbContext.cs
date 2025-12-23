using EcommerceAPI.Entities.Concrete;
using EcommerceAPI.Core.Interfaces;
using EcommerceAPI.DataAccess.Configurations;
using Microsoft.EntityFrameworkCore;

namespace EcommerceAPI.DataAccess.Concrete.EntityFramework.Contexts;

public class AppDbContext : DbContext
{
    private readonly IEncryptionService? _encryptionService;

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public AppDbContext(DbContextOptions<AppDbContext> options, IEncryptionService encryptionService) 
        : base(options)
    {
        _encryptionService = encryptionService;
    }
    
    // DbSets
    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Inventory> Inventories => Set<Inventory>();
    public DbSet<InventoryMovement> InventoryMovements => Set<InventoryMovement>();
    public DbSet<Cart> Carts => Set<Cart>();
    public DbSet<CartItem> CartItems => Set<CartItem>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<ShippingAddress> ShippingAddresses => Set<ShippingAddress>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // Şifreleme gerektirmeyen konfigürasyonları otomatik uygula
        modelBuilder.ApplyConfiguration(new RoleConfiguration());
        modelBuilder.ApplyConfiguration(new CategoryConfiguration());
        modelBuilder.ApplyConfiguration(new ProductConfiguration());
        modelBuilder.ApplyConfiguration(new InventoryConfiguration());
        modelBuilder.ApplyConfiguration(new InventoryMovementConfiguration());
        modelBuilder.ApplyConfiguration(new CartConfiguration());
        modelBuilder.ApplyConfiguration(new CartItemConfiguration());
        modelBuilder.ApplyConfiguration(new OrderItemConfiguration());
        modelBuilder.ApplyConfiguration(new PaymentConfiguration());
        modelBuilder.ApplyConfiguration(new RefreshTokenConfiguration());
        
        // Şifreleme gerektiren konfigürasyonlar (KVKK uyumlu)
        if (_encryptionService != null)
        {
            modelBuilder.ApplyConfiguration(new UserConfiguration(_encryptionService));
            modelBuilder.ApplyConfiguration(new ShippingAddressConfiguration(_encryptionService));
            modelBuilder.ApplyConfiguration(new OrderConfiguration(_encryptionService));
        }
        else
        {
            modelBuilder.ApplyConfiguration(new UserConfigurationBasic());
            modelBuilder.ApplyConfiguration(new ShippingAddressConfigurationBasic());
            modelBuilder.ApplyConfiguration(new OrderConfigurationBasic());
        }
    }
}
