using EcommerceAPI.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EcommerceAPI.Data.Configurations;

public class InventoryConfiguration : IEntityTypeConfiguration<Inventory>
{
    public void Configure(EntityTypeBuilder<Inventory> builder)
    {
        builder.ToTable("Inventories");
        
        builder.HasKey(i => i.ProductId);
        
        builder.Property(i => i.QuantityAvailable)
            .IsRequired();
        
        builder.Property(i => i.QuantityReserved)
            .IsRequired();
        
        // Concurrency token
        builder.Property(i => i.RowVersion)
            .IsRowVersion();
        
    }
}
