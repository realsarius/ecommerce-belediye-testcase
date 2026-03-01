using EcommerceAPI.Entities.Concrete;
using EcommerceAPI.Core.Interfaces;
using EcommerceAPI.DataAccess.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EcommerceAPI.DataAccess.Configurations;

public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    private readonly IEncryptionService _encryptionService;

    public OrderConfiguration(IEncryptionService encryptionService)
    {
        _encryptionService = encryptionService;
    }

    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("TBL_Orders");
        
        builder.HasKey(o => o.Id);
        
        builder.Property(o => o.OrderNumber)
            .IsRequired()
            .HasMaxLength(50);
        
        builder.HasIndex(o => o.OrderNumber)
            .IsUnique();
        
        builder.Property(o => o.TotalAmount)
            .IsRequired()
            .HasPrecision(18, 2);

        builder.Property(o => o.SubtotalAmount)
            .IsRequired()
            .HasPrecision(18, 2);

        builder.Property(o => o.LoyaltyDiscountAmount)
            .IsRequired()
            .HasPrecision(18, 2);
        
        builder.Property(o => o.Currency)
            .IsRequired()
            .HasMaxLength(3);
        
        // KVKK: ShippingAddress kişisel veri içerdiği için şifrelenir
        builder.Property(o => o.ShippingAddress)
            .HasMaxLength(2000)
            .HasConversion(new EncryptedStringConverter(_encryptionService));
        
        builder.HasIndex(o => o.UserId);
        builder.HasIndex(o => o.Status);
        builder.HasIndex(o => o.CreatedAt);
        
        // Relationships
        builder.HasMany(o => o.OrderItems)
            .WithOne(oi => oi.Order)
            .HasForeignKey(oi => oi.OrderId)
            .OnDelete(DeleteBehavior.Cascade);
        
        builder.HasOne(o => o.Payment)
            .WithOne(p => p.Order)
            .HasForeignKey<Payment>(p => p.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(o => o.LoyaltyTransactions)
            .WithOne(t => t.Order)
            .HasForeignKey(t => t.OrderId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
