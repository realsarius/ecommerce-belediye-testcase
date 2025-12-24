using EcommerceAPI.Entities.Concrete;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EcommerceAPI.DataAccess.Configurations;

public class InventoryMovementConfiguration : IEntityTypeConfiguration<InventoryMovement>
{
    public void Configure(EntityTypeBuilder<InventoryMovement> builder)
    {
        builder.ToTable("TBL_InventoryMovements");
        
        builder.HasKey(im => im.Id);
        
        builder.Property(im => im.Delta)
            .IsRequired();
        
        builder.Property(im => im.Reason)
            .IsRequired()
            .HasMaxLength(100);
        
        builder.Property(im => im.Notes)
            .HasMaxLength(1000);
        
        builder.HasIndex(im => im.ProductId);
        builder.HasIndex(im => im.CreatedAt);
        
        // Relationships
        builder.HasOne(im => im.Product)
            .WithMany(p => p.InventoryMovements)
            .HasForeignKey(im => im.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
        
        builder.HasOne(im => im.User)
            .WithMany(u => u.InventoryMovements)
            .HasForeignKey(im => im.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
