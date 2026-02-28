using EcommerceAPI.Entities.Concrete;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EcommerceAPI.DataAccess.Configurations;

public class WishlistItemConfiguration : IEntityTypeConfiguration<WishlistItem>
{
    public void Configure(EntityTypeBuilder<WishlistItem> builder)
    {
        builder.ToTable("TBL_WishlistItems");

        builder.HasKey(wi => wi.Id);

        // A user shouldn't add the same product multiple times to the wishlist
        builder.HasIndex(wi => new { wi.WishlistId, wi.ProductId })
            .IsUnique();

        builder.HasOne(wi => wi.Product)
            .WithMany(p => p.WishlistItems)
            .HasForeignKey(wi => wi.ProductId)
            .OnDelete(DeleteBehavior.Restrict); // Changed from Cascade to Restrict

        builder.Property(wi => wi.AddedAtPrice)
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder.Property(wi => wi.AddedAt)
            .IsRequired();
    }
}
