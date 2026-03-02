using EcommerceAPI.Entities.Concrete;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EcommerceAPI.DataAccess.Configurations;

public class GiftCardConfiguration : IEntityTypeConfiguration<GiftCard>
{
    public void Configure(EntityTypeBuilder<GiftCard> builder)
    {
        builder.ToTable("TBL_GiftCards");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Code)
            .IsRequired()
            .HasMaxLength(64);

        builder.HasIndex(x => x.Code)
            .IsUnique();

        builder.Property(x => x.InitialBalance)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(x => x.CurrentBalance)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(x => x.Currency)
            .IsRequired()
            .HasMaxLength(3);

        builder.Property(x => x.Description)
            .HasMaxLength(500);

        builder.HasIndex(x => x.AssignedUserId);
        builder.HasIndex(x => x.ExpiresAt);
        builder.HasIndex(x => x.IsActive);

        builder.HasOne(x => x.AssignedUser)
            .WithMany(x => x.GiftCards)
            .HasForeignKey(x => x.AssignedUserId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
