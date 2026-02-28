using EcommerceAPI.Entities.Concrete;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EcommerceAPI.DataAccess.Configurations;

public class WishlistCollectionConfiguration : IEntityTypeConfiguration<WishlistCollection>
{
    public void Configure(EntityTypeBuilder<WishlistCollection> builder)
    {
        builder.ToTable("TBL_WishlistCollections");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name)
            .HasMaxLength(80)
            .IsRequired();

        builder.Property(x => x.IsDefault)
            .HasDefaultValue(false);

        builder.HasIndex(x => new { x.WishlistId, x.Name })
            .IsUnique();

        builder.HasIndex(x => x.WishlistId)
            .HasFilter("\"IsDefault\" = true")
            .IsUnique();

        builder.HasOne(x => x.Wishlist)
            .WithMany(x => x.Collections)
            .HasForeignKey(x => x.WishlistId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
