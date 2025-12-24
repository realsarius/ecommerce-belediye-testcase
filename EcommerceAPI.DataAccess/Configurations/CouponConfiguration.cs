using EcommerceAPI.Entities.Concrete;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EcommerceAPI.DataAccess.Configurations;

public class CouponConfiguration : IEntityTypeConfiguration<Coupon>
{
    public void Configure(EntityTypeBuilder<Coupon> builder)
    {
        builder.ToTable("TBL_Coupons");
        
        builder.HasKey(c => c.Id);
        
        builder.Property(c => c.Code)
            .IsRequired()
            .HasMaxLength(50);
        
        builder.Property(c => c.Type)
            .IsRequired();
        
        builder.Property(c => c.Value)
            .IsRequired()
            .HasPrecision(18, 2);
        
        builder.Property(c => c.MinOrderAmount)
            .HasPrecision(18, 2);
        
        builder.Property(c => c.UsageLimit)
            .IsRequired();
        
        builder.Property(c => c.UsedCount)
            .IsRequired();
        
        builder.Property(c => c.ExpiresAt)
            .IsRequired();
        
        builder.Property(c => c.IsActive)
            .IsRequired();
        
        builder.Property(c => c.Description)
            .HasMaxLength(500);
        
        // Unique index on Code
        builder.HasIndex(c => c.Code)
            .IsUnique();
        
        // Index for active coupons query
        builder.HasIndex(c => new { c.IsActive, c.ExpiresAt });
    }
}
