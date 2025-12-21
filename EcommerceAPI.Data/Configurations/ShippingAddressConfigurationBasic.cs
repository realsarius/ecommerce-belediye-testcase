using EcommerceAPI.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EcommerceAPI.Data.Configurations;

public class ShippingAddressConfigurationBasic : IEntityTypeConfiguration<ShippingAddress>
{
    public void Configure(EntityTypeBuilder<ShippingAddress> builder)
    {
        builder.ToTable("TBL_ShippingAddresses");
        
        builder.HasKey(sa => sa.Id);
        
        builder.Property(sa => sa.Title)
            .IsRequired()
            .HasMaxLength(300);
        
        builder.Property(sa => sa.FullName)
            .IsRequired()
            .HasMaxLength(500);
        
        builder.Property(sa => sa.Phone)
            .IsRequired()
            .HasMaxLength(300);
        
        builder.Property(sa => sa.City)
            .IsRequired()
            .HasMaxLength(300);
        
        builder.Property(sa => sa.District)
            .IsRequired()
            .HasMaxLength(300);
        
        builder.Property(sa => sa.AddressLine)
            .IsRequired()
            .HasMaxLength(1000);
        
        builder.Property(sa => sa.PostalCode)
            .HasMaxLength(200);
        
        builder.Property(sa => sa.IsDefault)
            .IsRequired();
        
        builder.HasIndex(sa => sa.UserId);
        
        builder.HasOne(sa => sa.User)
            .WithMany(u => u.ShippingAddresses)
            .HasForeignKey(sa => sa.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
