using EcommerceAPI.Entities.Concrete;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EcommerceAPI.DataAccess.Configurations;

public class CampaignProductConfiguration : IEntityTypeConfiguration<CampaignProduct>
{
    public void Configure(EntityTypeBuilder<CampaignProduct> builder)
    {
        builder.ToTable("TBL_CampaignProducts");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.CampaignPrice)
            .HasColumnType("decimal(18,2)");

        builder.Property(x => x.OriginalPriceSnapshot)
            .HasColumnType("decimal(18,2)");

        builder.HasIndex(x => new { x.CampaignId, x.ProductId })
            .IsUnique();

        builder.HasOne(x => x.Campaign)
            .WithMany(x => x.CampaignProducts)
            .HasForeignKey(x => x.CampaignId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Product)
            .WithMany(x => x.CampaignProducts)
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
