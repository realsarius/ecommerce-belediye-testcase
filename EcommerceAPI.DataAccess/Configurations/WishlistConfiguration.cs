using EcommerceAPI.Entities.Concrete;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EcommerceAPI.DataAccess.Configurations;

public class WishlistConfiguration : IEntityTypeConfiguration<Wishlist>
{
    public void Configure(EntityTypeBuilder<Wishlist> builder)
    {
        builder.ToTable("TBL_Wishlists");
        
        builder.HasKey(w => w.Id);
        
        builder.HasIndex(w => w.UserId)
            .IsUnique(); // One wishlist per user

        builder.HasIndex(w => w.ShareToken)
            .IsUnique();

        builder.Property(w => w.IsPublic)
            .HasDefaultValue(false);
            
        builder.HasOne(w => w.User)
            .WithOne(u => u.Wishlist)
            .HasForeignKey<Wishlist>(w => w.UserId)
            .OnDelete(DeleteBehavior.Cascade);
            
        builder.HasMany(w => w.Items)
            .WithOne(wi => wi.Wishlist)
            .HasForeignKey(wi => wi.WishlistId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
