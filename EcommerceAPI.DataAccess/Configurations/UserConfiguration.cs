using EcommerceAPI.Entities.Concrete;
using EcommerceAPI.Core.Interfaces;
using EcommerceAPI.DataAccess.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EcommerceAPI.DataAccess.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    private readonly IEncryptionService _encryptionService;

    public UserConfiguration(IEncryptionService encryptionService)
    {
        _encryptionService = encryptionService;
    }

    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("TBL_Users");
        
        builder.HasKey(u => u.Id);
        
        builder.Property(u => u.Email)
            .IsRequired()
            .HasMaxLength(500)
            .HasConversion(new EncryptedStringConverter(_encryptionService));
        
        builder.Property(u => u.EmailHash)
            .IsRequired()
            .HasMaxLength(64);
        
        builder.HasIndex(u => u.EmailHash)
            .IsUnique();
        
        builder.Property(u => u.PasswordHash)
            .IsRequired()
            .HasMaxLength(500);
        
        builder.Property(u => u.FirstName)
            .IsRequired()
            .HasMaxLength(300)
            .HasConversion(new EncryptedStringConverter(_encryptionService));
        
        builder.Property(u => u.LastName)
            .IsRequired()
            .HasMaxLength(300)
            .HasConversion(new EncryptedStringConverter(_encryptionService));
        
        // Relationships
        builder.HasOne(u => u.Role)
            .WithMany(r => r.Users)
            .HasForeignKey(u => u.RoleId)
            .OnDelete(DeleteBehavior.Restrict);
        
        builder.HasOne(u => u.Cart)
            .WithOne(c => c.User)
            .HasForeignKey<Cart>(c => c.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        
        builder.HasMany(u => u.Orders)
            .WithOne(o => o.User)
            .HasForeignKey(o => o.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
