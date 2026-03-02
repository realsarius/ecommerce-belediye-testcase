using EcommerceAPI.Entities.Concrete;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EcommerceAPI.DataAccess.Configurations;

public class NotificationTemplateSettingConfiguration : IEntityTypeConfiguration<NotificationTemplateSetting>
{
    public void Configure(EntityTypeBuilder<NotificationTemplateSetting> builder)
    {
        builder.ToTable("TBL_NotificationTemplateSettings");

        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.Type).IsUnique();

        builder.Property(x => x.Type)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(x => x.DisplayName)
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(x => x.Description)
            .HasMaxLength(512)
            .IsRequired();

        builder.Property(x => x.TitleExample)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(x => x.BodyExample)
            .HasMaxLength(1024)
            .IsRequired();
    }
}
