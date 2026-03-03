using EcommerceAPI.Entities.Concrete;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EcommerceAPI.DataAccess.Configurations;

public class InvoiceInfoConfigurationBasic : IEntityTypeConfiguration<InvoiceInfo>
{
    public void Configure(EntityTypeBuilder<InvoiceInfo> builder)
    {
        builder.ToTable("TBL_InvoiceInfos");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.FullName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.TcKimlikNo)
            .HasMaxLength(32);

        builder.Property(x => x.CompanyName)
            .HasMaxLength(200);

        builder.Property(x => x.TaxOffice)
            .HasMaxLength(200);

        builder.Property(x => x.TaxNumber)
            .HasMaxLength(32);

        builder.Property(x => x.InvoiceAddress)
            .IsRequired()
            .HasMaxLength(2000);

        builder.HasIndex(x => x.OrderId)
            .IsUnique();

        builder.HasOne(x => x.Order)
            .WithOne(o => o.InvoiceInfo)
            .HasForeignKey<InvoiceInfo>(x => x.OrderId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
