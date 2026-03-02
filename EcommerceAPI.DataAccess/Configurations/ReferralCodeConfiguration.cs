using EcommerceAPI.Entities.Concrete;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EcommerceAPI.DataAccess.Configurations;

public class ReferralCodeConfiguration : IEntityTypeConfiguration<ReferralCode>
{
    public void Configure(EntityTypeBuilder<ReferralCode> builder)
    {
        builder.ToTable("TBL_ReferralCodes");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Code)
            .IsRequired()
            .HasMaxLength(32);

        builder.HasIndex(x => x.Code)
            .IsUnique();

        builder.HasIndex(x => x.UserId)
            .IsUnique();

        builder.Property(x => x.IsActive)
            .HasDefaultValue(true);

        builder.HasOne(x => x.User)
            .WithOne(x => x.ReferralCode)
            .HasForeignKey<ReferralCode>(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
