using EcommerceAPI.Entities.DTOs;
using FluentValidation;

namespace EcommerceAPI.Business.Validators;

public class PresignMediaUploadRequestValidator : AbstractValidator<PresignMediaUploadRequest>
{
    public PresignMediaUploadRequestValidator()
    {
        RuleFor(x => x.Context)
            .NotEmpty().WithMessage("Upload context zorunludur")
            .Must(BeKnownContext).WithMessage("Geçersiz upload context");

        RuleFor(x => x.ReferenceId)
            .GreaterThanOrEqualTo(0).WithMessage("ReferenceId negatif olamaz");

        RuleFor(x => x.ContentType)
            .NotEmpty().WithMessage("Content type zorunludur")
            .Must(BeAllowedContentType).WithMessage("Sadece JPEG, PNG, WebP veya GIF yüklenebilir");

        RuleFor(x => x.FileSizeBytes)
            .GreaterThan(0).WithMessage("Dosya boyutu geçersiz")
            .LessThanOrEqualTo(10 * 1024 * 1024).WithMessage("Dosya boyutu 10 MB sınırını aşıyor");
    }

    private static bool BeKnownContext(string context)
    {
        var normalized = context.Trim().ToLowerInvariant();
        return normalized is "product" or "category" or "seller-logo" or "seller-banner";
    }

    private static bool BeAllowedContentType(string contentType)
    {
        var normalized = contentType.Trim().ToLowerInvariant();
        return normalized is "image/jpeg" or "image/png" or "image/webp" or "image/gif";
    }
}
