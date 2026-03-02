using EcommerceAPI.Entities.Concrete;
using EcommerceAPI.Entities.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EcommerceAPI.DataAccess.Configurations;

public class ProductReviewConfiguration : IEntityTypeConfiguration<ProductReview>
{
    public void Configure(EntityTypeBuilder<ProductReview> builder)
    {
        builder.HasIndex(r => new { r.UserId, r.ProductId }).IsUnique(); // 1 kullanıcı 1 ürüne 1 yorum
        builder.Property(r => r.Rating).IsRequired();
        builder.Property(r => r.Comment).HasMaxLength(1000);
        builder.Property(r => r.SellerReply).HasMaxLength(1500);
        builder.Property(r => r.ModerationStatus)
            .HasConversion<int>()
            .HasDefaultValue(ProductReviewModerationStatus.Approved);
        builder.Property(r => r.ModerationNote).HasMaxLength(1000);
        builder.HasIndex(r => r.ModerationStatus);

        builder.HasOne(r => r.Product)
            .WithMany(p => p.Reviews)
            .HasForeignKey(r => r.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(r => r.User)
            .WithMany()
            .HasForeignKey(r => r.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
