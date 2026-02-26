using EcommerceAPI.Entities.Concrete;
using EcommerceAPI.Core.Interfaces;
using EcommerceAPI.DataAccess.Configurations;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using EcommerceAPI.Core.CrossCuttingConcerns.Logging;
using System.Text.Json;

namespace EcommerceAPI.DataAccess.Concrete.EntityFramework.Contexts;

public class AppDbContext : DbContext
{
    private readonly IEncryptionService? _encryptionService;
    private readonly IAuditService? _auditService;
    private readonly IHttpContextAccessor? _httpContextAccessor;

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public AppDbContext(
        DbContextOptions<AppDbContext> options,
        IEncryptionService encryptionService,
        IAuditService auditService,
        IHttpContextAccessor httpContextAccessor)
        : base(options)
    {
        _encryptionService = encryptionService;
        _auditService = auditService;
        _httpContextAccessor = httpContextAccessor;
    }
    

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
    public DbSet<Coupon> Coupons => Set<Coupon>();
    public DbSet<SellerProfile> SellerProfiles => Set<SellerProfile>();
    public DbSet<CreditCard> CreditCards => Set<CreditCard>();
    public DbSet<SupportConversation> SupportConversations => Set<SupportConversation>();
    public DbSet<SupportMessage> SupportMessages => Set<SupportMessage>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    public DbSet<InboxMessage> InboxMessages => Set<InboxMessage>();
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

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
        modelBuilder.ApplyConfiguration(new SellerProfileConfiguration());
        modelBuilder.ApplyConfiguration(new CouponConfiguration());
        modelBuilder.ApplyConfiguration(new SupportConversationConfiguration());
        modelBuilder.ApplyConfiguration(new SupportMessageConfiguration());
        modelBuilder.ApplyConfiguration(new OutboxMessageConfiguration());
        modelBuilder.ApplyConfiguration(new InboxMessageConfiguration());

        

        if (_encryptionService != null)
        {
            modelBuilder.ApplyConfiguration(new UserConfiguration(_encryptionService));
            modelBuilder.ApplyConfiguration(new ShippingAddressConfiguration(_encryptionService));
            modelBuilder.ApplyConfiguration(new OrderConfiguration(_encryptionService));
            modelBuilder.ApplyConfiguration(new CreditCardConfiguration(_encryptionService!));
        }
        else
        {
            modelBuilder.ApplyConfiguration(new UserConfigurationBasic());
            modelBuilder.ApplyConfiguration(new ShippingAddressConfigurationBasic());
            modelBuilder.ApplyConfiguration(new OrderConfigurationBasic());
            modelBuilder.ApplyConfiguration(new CreditCardConfigurationBasic());
        }
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var auditEntries = OnBeforeSaveChanges();
        var result = await base.SaveChangesAsync(cancellationToken);
        await OnAfterSaveChanges(auditEntries);
        return result;
    }

    private List<AuditEntry> OnBeforeSaveChanges()
    {
        ChangeTracker.DetectChanges();
        var auditEntries = new List<AuditEntry>();

        if (_auditService == null) return auditEntries;

        var userId = _httpContextAccessor?.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "System/Anonymous";

        foreach (var entry in ChangeTracker.Entries())
        {
            if (entry.State == EntityState.Detached || entry.State == EntityState.Unchanged)
                continue;

            var auditEntry = new AuditEntry(entry)
            {
                TableName = entry.Entity.GetType().Name,
                UserId = userId,
                Action = entry.State.ToString()
            };

            auditEntries.Add(auditEntry);

            foreach (var property in entry.Properties)
            {
                if (property.IsTemporary)
                {
                    auditEntry.TemporaryProperties.Add(property);
                    continue;
                }

                string propertyName = property.Metadata.Name;
                if (property.Metadata.IsPrimaryKey())
                {
                    auditEntry.KeyValues[propertyName] = property.CurrentValue;
                    continue;
                }

                switch (entry.State)
                {
                    case EntityState.Added:
                        auditEntry.NewValues[propertyName] = property.CurrentValue;
                        break;

                    case EntityState.Deleted:
                        auditEntry.OldValues[propertyName] = property.OriginalValue;
                        break;

                    case EntityState.Modified:
                        if (property.IsModified)
                        {
                            auditEntry.OldValues[propertyName] = property.OriginalValue;
                            auditEntry.NewValues[propertyName] = property.CurrentValue;
                        }
                        break;
                }
            }
        }

        foreach (var auditEntry in auditEntries.Where(e => !e.HasTemporaryProperties))
        {
            // ID'si belli olanları hemen işle dışarı
        }

        return auditEntries.Where(e => e.HasTemporaryProperties == false).ToList();
    }

    private async Task OnAfterSaveChanges(List<AuditEntry> auditEntries)
    {
        if (_auditService == null || auditEntries == null || !auditEntries.Any())
            return;

        foreach (var auditEntry in auditEntries)
        {
            // SaveChanges sonrası ID'si oluşanlar için update (Added state olanlar)
            foreach (var prop in auditEntry.TemporaryProperties)
            {
                if (prop.Metadata.IsPrimaryKey())
                {
                    auditEntry.KeyValues[prop.Metadata.Name] = prop.CurrentValue;
                }
                else
                {
                    auditEntry.NewValues[prop.Metadata.Name] = prop.CurrentValue;
                }
            }

            var changes = new Dictionary<string, object>
            {
                { "PrimaryKey", auditEntry.KeyValues },
                { "OldValues", auditEntry.OldValues },
                { "NewValues", auditEntry.NewValues }
            };

            if (auditEntry.OldValues.Count == 0 && auditEntry.NewValues.Count == 0 && auditEntry.Action == "Modified")
            {
                continue;
            }

            await _auditService.LogActionAsync(
                auditEntry.UserId,
                $"Entity{auditEntry.Action}",
                auditEntry.TableName,
                changes
            );
        }
    }
}

public class AuditEntry
{
    public AuditEntry(Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry entry)
    {
        Entry = entry;
    }

    public Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry Entry { get; }
    public string UserId { get; set; }
    public string TableName { get; set; }
    public string Action { get; set; }
    public Dictionary<string, object?> KeyValues { get; } = new();
    public Dictionary<string, object?> OldValues { get; } = new();
    public Dictionary<string, object?> NewValues { get; } = new();
    public List<Microsoft.EntityFrameworkCore.ChangeTracking.PropertyEntry> TemporaryProperties { get; } = new();

    public bool HasTemporaryProperties => TemporaryProperties.Any();
}
