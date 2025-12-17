using EcommerceAPI.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EcommerceAPI.Data.Configurations;

public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.ToTable("Payments");
        
        builder.HasKey(p => p.Id);
        
        builder.Property(p => p.Amount)
            .IsRequired()
            .HasPrecision(18, 2);
        
        builder.Property(p => p.Currency)
            .IsRequired()
            .HasMaxLength(3);
        
        builder.Property(p => p.PaymentMethod)
            .IsRequired()
            .HasMaxLength(50);
        
        builder.Property(p => p.IdempotencyKey)
            .IsRequired()
            .HasMaxLength(255);
        
        builder.HasIndex(p => p.IdempotencyKey)
            .IsUnique();
        
        builder.HasIndex(p => p.OrderId);
    }
}
