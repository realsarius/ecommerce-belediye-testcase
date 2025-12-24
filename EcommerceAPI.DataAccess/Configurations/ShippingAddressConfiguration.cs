using EcommerceAPI.Entities.Concrete;
using EcommerceAPI.Core.Interfaces;
using EcommerceAPI.DataAccess.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EcommerceAPI.DataAccess.Configurations;

public class ShippingAddressConfiguration : IEntityTypeConfiguration<ShippingAddress>
{
    private readonly IEncryptionService _encryptionService;

    public ShippingAddressConfiguration(IEncryptionService encryptionService)
    {
        _encryptionService = encryptionService;
    }

    public void Configure(EntityTypeBuilder<ShippingAddress> builder)
    {
        builder.ToTable("TBL_ShippingAddresses");
        
        builder.HasKey(sa => sa.Id);
        
        builder.Property(sa => sa.Title)
            .IsRequired()
            .HasMaxLength(300)
            .HasConversion(new EncryptedStringConverter(_encryptionService));
        
        builder.Property(sa => sa.FullName)
            .IsRequired()
            .HasMaxLength(500)
            .HasConversion(new EncryptedStringConverter(_encryptionService));
        
        builder.Property(sa => sa.Phone)
            .IsRequired()
            .HasMaxLength(300)
            .HasConversion(new EncryptedStringConverter(_encryptionService));
        
        builder.Property(sa => sa.City)
            .IsRequired()
            .HasMaxLength(300)
            .HasConversion(new EncryptedStringConverter(_encryptionService));
        
        builder.Property(sa => sa.District)
            .IsRequired()
            .HasMaxLength(300)
            .HasConversion(new EncryptedStringConverter(_encryptionService));
        
        builder.Property(sa => sa.AddressLine)
            .IsRequired()
            .HasMaxLength(1000)
            .HasConversion(new EncryptedStringConverter(_encryptionService));
        
        builder.Property(sa => sa.PostalCode)
            .HasMaxLength(200)
            .HasConversion(new NullableEncryptedStringConverter(_encryptionService));
        
        builder.Property(sa => sa.IsDefault)
            .IsRequired();
        
        builder.HasIndex(sa => sa.UserId);
        
        // Relationship
        builder.HasOne(sa => sa.User)
            .WithMany(u => u.ShippingAddresses)
            .HasForeignKey(sa => sa.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
