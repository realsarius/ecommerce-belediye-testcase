using EcommerceAPI.Entities.Concrete;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EcommerceAPI.DataAccess.Configurations;

public class ReturnRequestAttachmentConfiguration : IEntityTypeConfiguration<ReturnRequestAttachment>
{
    public void Configure(EntityTypeBuilder<ReturnRequestAttachment> builder)
    {
        builder.ToTable("TBL_ReturnRequestAttachments");

        builder.HasKey(attachment => attachment.Id);

        builder.Property(attachment => attachment.OriginalFileName)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(attachment => attachment.StoredFileName)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(attachment => attachment.RelativePath)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(attachment => attachment.ContentType)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(attachment => attachment.SizeBytes)
            .IsRequired();

        builder.HasIndex(attachment => attachment.ReturnRequestId);

        builder.HasOne(attachment => attachment.ReturnRequest)
            .WithMany(request => request.Attachments)
            .HasForeignKey(attachment => attachment.ReturnRequestId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
