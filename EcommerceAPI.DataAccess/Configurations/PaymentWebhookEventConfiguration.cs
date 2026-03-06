using EcommerceAPI.Entities.Concrete;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EcommerceAPI.DataAccess.Configurations;

public class PaymentWebhookEventConfiguration : IEntityTypeConfiguration<PaymentWebhookEvent>
{
    public void Configure(EntityTypeBuilder<PaymentWebhookEvent> builder)
    {
        builder.ToTable("TBL_PaymentWebhookEvents");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Provider)
            .IsRequired();

        builder.Property(x => x.DedupeKey)
            .IsRequired()
            .HasMaxLength(128);

        builder.Property(x => x.EventType)
            .IsRequired()
            .HasMaxLength(120);

        builder.Property(x => x.ProviderEventId)
            .HasMaxLength(150);

        builder.Property(x => x.PaymentId)
            .HasMaxLength(150);

        builder.Property(x => x.PaymentConversationId)
            .HasMaxLength(150);

        builder.Property(x => x.Status)
            .HasMaxLength(40);

        builder.Property(x => x.EventTime)
            .HasMaxLength(80);

        builder.HasIndex(x => new { x.Provider, x.DedupeKey })
            .IsUnique();

        builder.HasIndex(x => x.CreatedAt);
        builder.HasIndex(x => x.PaymentConversationId);
    }
}
