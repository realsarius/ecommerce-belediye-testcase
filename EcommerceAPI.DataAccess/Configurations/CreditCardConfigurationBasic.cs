using EcommerceAPI.Entities.Concrete;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EcommerceAPI.DataAccess.Configurations;

public class CreditCardConfigurationBasic : IEntityTypeConfiguration<CreditCard>
{
    public void Configure(EntityTypeBuilder<CreditCard> builder)
    {
        builder.ToTable("TBL_CreditCards");
        
        builder.HasKey(cc => cc.Id);
        
        builder.Property(cc => cc.CardAlias)
            .IsRequired()
            .HasMaxLength(200);
        
        builder.Property(cc => cc.CardHolderName)
            .IsRequired()
            .HasMaxLength(500);
        
        builder.Property(cc => cc.CardNumberEncrypted)
            .IsRequired()
            .HasMaxLength(500);
        
        builder.Property(cc => cc.Last4Digits)
            .IsRequired()
            .HasMaxLength(4);
        
        builder.Property(cc => cc.ExpireYearEncrypted)
            .IsRequired()
            .HasMaxLength(200);
        
        builder.Property(cc => cc.ExpireMonthEncrypted)
            .IsRequired()
            .HasMaxLength(200);
        
        builder.Property(cc => cc.CvvEncrypted)
            .IsRequired()
            .HasMaxLength(200);
        
        builder.Property(cc => cc.IsDefault)
            .IsRequired();
        
        builder.HasIndex(cc => cc.UserId);
        
        builder.HasOne(cc => cc.User)
            .WithMany(u => u.CreditCards)
            .HasForeignKey(cc => cc.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
