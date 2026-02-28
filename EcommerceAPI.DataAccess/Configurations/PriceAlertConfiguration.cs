using EcommerceAPI.Entities.Concrete;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EcommerceAPI.DataAccess.Configurations;

public class PriceAlertConfiguration : IEntityTypeConfiguration<PriceAlert>
{
    public void Configure(EntityTypeBuilder<PriceAlert> builder)
    {
        builder.ToTable("TBL_PriceAlerts");

        builder.HasKey(pa => pa.Id);

        builder.HasIndex(pa => new { pa.UserId, pa.ProductId })
            .IsUnique();

        builder.HasIndex(pa => new { pa.IsActive, pa.ProductId });

        builder.Property(pa => pa.TargetPrice)
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder.Property(pa => pa.LastKnownPrice)
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder.Property(pa => pa.LastTriggeredPrice)
            .HasColumnType("decimal(18,2)");

        builder.HasOne(pa => pa.User)
            .WithMany()
            .HasForeignKey(pa => pa.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(pa => pa.Product)
            .WithMany()
            .HasForeignKey(pa => pa.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
