using EcommerceAPI.Entities.Concrete;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EcommerceAPI.DataAccess.Configurations;

public class AnnouncementConfiguration : IEntityTypeConfiguration<Announcement>
{
    public void Configure(EntityTypeBuilder<Announcement> builder)
    {
        builder.HasKey(announcement => announcement.Id);

        builder.Property(announcement => announcement.Title)
            .HasMaxLength(160)
            .IsRequired();

        builder.Property(announcement => announcement.Message)
            .HasMaxLength(4000)
            .IsRequired();

        builder.Property(announcement => announcement.TargetRole)
            .HasMaxLength(64);

        builder.Property(announcement => announcement.TargetUserIds)
            .HasMaxLength(2000);

        builder.Property(announcement => announcement.Channels)
            .HasMaxLength(128)
            .IsRequired();

        builder.HasIndex(announcement => announcement.CreatedByUserId);
        builder.HasIndex(announcement => announcement.CreatedAt);
        builder.HasIndex(announcement => announcement.ScheduledAt);

        builder.HasOne(announcement => announcement.CreatedByUser)
            .WithMany()
            .HasForeignKey(announcement => announcement.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
