using EcommerceAPI.Entities.Concrete;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EcommerceAPI.DataAccess.Configurations;

public class ReferralTransactionConfiguration : IEntityTypeConfiguration<ReferralTransaction>
{
    public void Configure(EntityTypeBuilder<ReferralTransaction> builder)
    {
        builder.ToTable("TBL_ReferralTransactions");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Description)
            .IsRequired()
            .HasMaxLength(500);

        builder.HasIndex(x => x.ReferrerUserId);
        builder.HasIndex(x => x.ReferredUserId);
        builder.HasIndex(x => x.BeneficiaryUserId);
        builder.HasIndex(x => x.OrderId);
        builder.HasIndex(x => x.Type);
        builder.HasIndex(x => x.CreatedAt);

        builder.HasOne(x => x.ReferralCode)
            .WithMany(x => x.ReferralTransactions)
            .HasForeignKey(x => x.ReferralCodeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.ReferrerUser)
            .WithMany(x => x.ReferralTransactionsAsReferrer)
            .HasForeignKey(x => x.ReferrerUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.ReferredUser)
            .WithMany(x => x.ReferralTransactionsAsReferred)
            .HasForeignKey(x => x.ReferredUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.BeneficiaryUser)
            .WithMany(x => x.ReferralTransactionsAsBeneficiary)
            .HasForeignKey(x => x.BeneficiaryUserId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(x => x.Order)
            .WithMany(x => x.ReferralTransactions)
            .HasForeignKey(x => x.OrderId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
