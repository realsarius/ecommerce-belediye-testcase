using EcommerceAPI.Entities.Concrete;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EcommerceAPI.DataAccess.Configurations;

public class SellerProfileConfiguration : IEntityTypeConfiguration<SellerProfile>
{
    public void Configure(EntityTypeBuilder<SellerProfile> builder)
    {
        builder.ToTable("TBL_SellerProfiles");
        
        builder.HasKey(sp => sp.Id);
        
        builder.Property(sp => sp.BrandName)
            .IsRequired()
            .HasMaxLength(200);
        
        builder.Property(sp => sp.BrandDescription)
            .HasMaxLength(2000);
        
        builder.Property(sp => sp.LogoUrl)
            .HasMaxLength(500);
        
        builder.Property(sp => sp.IsVerified)
            .HasDefaultValue(false);
        
        // Unique constraint on UserId (1-to-1 with User)
        builder.HasIndex(sp => sp.UserId)
            .IsUnique();
        
        builder.HasIndex(sp => sp.BrandName);
        
        // Relationships
        builder.HasOne(sp => sp.User)
            .WithOne(u => u.SellerProfile)
            .HasForeignKey<SellerProfile>(sp => sp.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        
        builder.HasMany(sp => sp.Products)
            .WithOne(p => p.Seller)
            .HasForeignKey(p => p.SellerId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
