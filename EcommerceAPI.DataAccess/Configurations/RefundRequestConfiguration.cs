using EcommerceAPI.Entities.Concrete;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EcommerceAPI.DataAccess.Configurations;

public class RefundRequestConfiguration : IEntityTypeConfiguration<RefundRequest>
{
    public void Configure(EntityTypeBuilder<RefundRequest> builder)
    {
        builder.ToTable("TBL_RefundRequests");

        builder.HasKey(rr => rr.Id);

        builder.Property(rr => rr.Amount)
            .IsRequired()
            .HasPrecision(18, 2);

        builder.Property(rr => rr.IdempotencyKey)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(rr => rr.ProviderRefundId)
            .HasMaxLength(150);

        builder.Property(rr => rr.FailureReason)
            .HasMaxLength(1000);

        builder.HasIndex(rr => rr.ReturnRequestId)
            .IsUnique();

        builder.HasIndex(rr => rr.IdempotencyKey)
            .IsUnique();

        builder.HasOne(rr => rr.ReturnRequest)
            .WithOne(r => r.RefundRequest)
            .HasForeignKey<RefundRequest>(rr => rr.ReturnRequestId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(rr => rr.Order)
            .WithMany()
            .HasForeignKey(rr => rr.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(rr => rr.Payment)
            .WithMany()
            .HasForeignKey(rr => rr.PaymentId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
