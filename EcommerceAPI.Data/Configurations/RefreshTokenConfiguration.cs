using EcommerceAPI.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EcommerceAPI.Data.Configurations;

public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("TBL_RefreshTokens");
        
        builder.HasKey(rt => rt.Id);

        builder.Property(rt => rt.Token)
            .IsRequired()
            .HasMaxLength(200); // Hashed token length

        builder.Property(rt => rt.JwtId)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(rt => rt.RevokedReason)
            .HasMaxLength(200);

        builder.Property(rt => rt.ReplacedByToken)
            .HasMaxLength(200);

        // Index for faster lookup by Token
        builder.HasIndex(rt => rt.Token)
            .IsUnique();
        
        // Relationship
        builder.HasOne(rt => rt.User)
            .WithMany() // User entity'sine collection eklemedik, gerekirse ekleyebiliriz veya tek yönlü kalabilir
            .HasForeignKey(rt => rt.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
