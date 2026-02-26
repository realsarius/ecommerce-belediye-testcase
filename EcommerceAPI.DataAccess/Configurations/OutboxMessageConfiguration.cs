using EcommerceAPI.Entities.Concrete;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EcommerceAPI.DataAccess.Configurations;

public class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("TBL_OutboxMessages");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.EventId).IsRequired();

        builder.Property(x => x.EventType)
            .IsRequired()
            .HasMaxLength(512);

        builder.Property(x => x.Payload)
            .IsRequired();

        builder.Property(x => x.RetryCount)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(x => x.LastError)
            .HasMaxLength(2000);

        builder.HasIndex(x => x.EventId).IsUnique();
        builder.HasIndex(x => x.ProcessedOnUtc);
        builder.HasIndex(x => x.CreatedAt);
        builder.HasIndex(x => x.RetryCount);
    }
}
