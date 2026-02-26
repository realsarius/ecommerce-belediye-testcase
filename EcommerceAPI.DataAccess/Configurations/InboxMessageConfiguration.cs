using EcommerceAPI.Entities.Concrete;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EcommerceAPI.DataAccess.Configurations;

public class InboxMessageConfiguration : IEntityTypeConfiguration<InboxMessage>
{
    public void Configure(EntityTypeBuilder<InboxMessage> builder)
    {
        builder.ToTable("TBL_InboxMessages");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.MessageId).IsRequired();

        builder.Property(x => x.ConsumerName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.MessageType)
            .IsRequired()
            .HasMaxLength(512);

        builder.Property(x => x.ProcessedOnUtc).IsRequired();

        builder.HasIndex(x => new { x.ConsumerName, x.MessageId }).IsUnique();
        builder.HasIndex(x => x.ProcessedOnUtc);
    }
}
