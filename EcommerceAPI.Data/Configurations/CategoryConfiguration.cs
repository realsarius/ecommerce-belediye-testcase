using EcommerceAPI.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EcommerceAPI.Data.Configurations;

public class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.ToTable("TBL_Categories");
        
        builder.HasKey(c => c.Id);
        
        builder.Property(c => c.Name)
            .IsRequired()
            .HasMaxLength(255);
        
        builder.HasIndex(c => c.Name)
            .IsUnique();
        
        builder.Property(c => c.Description)
            .HasMaxLength(1000);
        
        builder.Property(c => c.IsActive)
            .IsRequired();
    }
}
