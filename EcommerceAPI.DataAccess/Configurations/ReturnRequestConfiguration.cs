using EcommerceAPI.Entities.Concrete;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EcommerceAPI.DataAccess.Configurations;

public class ReturnRequestConfiguration : IEntityTypeConfiguration<ReturnRequest>
{
    public void Configure(EntityTypeBuilder<ReturnRequest> builder)
    {
        builder.ToTable("TBL_ReturnRequests");

        builder.HasKey(rr => rr.Id);

        builder.Property(rr => rr.Reason)
            .IsRequired()
            .HasMaxLength(250);

        builder.Property(rr => rr.RequestNote)
            .HasMaxLength(1000);

        builder.Property(rr => rr.ReviewNote)
            .HasMaxLength(1000);

        builder.Property(rr => rr.RequestedRefundAmount)
            .IsRequired()
            .HasPrecision(18, 2);

        builder.HasIndex(rr => rr.OrderId);
        builder.HasIndex(rr => rr.UserId);
        builder.HasIndex(rr => rr.Status);

        builder.HasOne(rr => rr.Order)
            .WithMany()
            .HasForeignKey(rr => rr.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(rr => rr.User)
            .WithMany()
            .HasForeignKey(rr => rr.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(rr => rr.ReviewedByUser)
            .WithMany()
            .HasForeignKey(rr => rr.ReviewedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
