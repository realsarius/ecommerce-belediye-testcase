using EcommerceAPI.Core.Interfaces;
using EcommerceAPI.DataAccess.Converters;
using EcommerceAPI.Entities.Concrete;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EcommerceAPI.DataAccess.Configurations;

public class InvoiceInfoConfiguration : IEntityTypeConfiguration<InvoiceInfo>
{
    private readonly IEncryptionService _encryptionService;

    public InvoiceInfoConfiguration(IEncryptionService encryptionService)
    {
        _encryptionService = encryptionService;
    }

    public void Configure(EntityTypeBuilder<InvoiceInfo> builder)
    {
        builder.ToTable("TBL_InvoiceInfos");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.FullName)
            .IsRequired()
            .HasMaxLength(200)
            .HasConversion(new EncryptedStringConverter(_encryptionService));

        builder.Property(x => x.TcKimlikNo)
            .HasMaxLength(32)
            .HasConversion(new NullableEncryptedStringConverter(_encryptionService));

        builder.Property(x => x.CompanyName)
            .HasMaxLength(200)
            .HasConversion(new NullableEncryptedStringConverter(_encryptionService));

        builder.Property(x => x.TaxOffice)
            .HasMaxLength(200)
            .HasConversion(new NullableEncryptedStringConverter(_encryptionService));

        builder.Property(x => x.TaxNumber)
            .HasMaxLength(32)
            .HasConversion(new NullableEncryptedStringConverter(_encryptionService));

        builder.Property(x => x.InvoiceAddress)
            .IsRequired()
            .HasMaxLength(2000)
            .HasConversion(new EncryptedStringConverter(_encryptionService));

        builder.HasIndex(x => x.OrderId)
            .IsUnique();

        builder.HasOne(x => x.Order)
            .WithOne(o => o.InvoiceInfo)
            .HasForeignKey<InvoiceInfo>(x => x.OrderId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
