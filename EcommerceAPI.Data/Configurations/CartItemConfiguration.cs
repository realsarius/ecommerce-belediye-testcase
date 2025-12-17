using EcommerceAPI.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EcommerceAPI.Data.Configurations;

public class CartItemConfiguration : IEntityTypeConfiguration<CartItem>
{
    public void Configure(EntityTypeBuilder<CartItem> builder)
    {
        builder.ToTable("CartItems");
        
        builder.HasKey(ci => ci.Id);
        
        builder.Property(ci => ci.Quantity)
            .IsRequired();
        
        builder.Property(ci => ci.PriceSnapshot)
            .IsRequired()
            .HasPrecision(18, 2);
        
        builder.HasIndex(ci => new { ci.CartId, ci.ProductId })
            .IsUnique();
        
        // Relationship
        builder.HasOne(ci => ci.Product)
            .WithMany(p => p.CartItems)
            .HasForeignKey(ci => ci.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
