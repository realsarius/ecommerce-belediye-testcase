using EcommerceAPI.Core.Entities;

namespace EcommerceAPI.Entities.DTOs;

public class PresignMediaUploadRequest : IDto
{
    public string Context { get; set; } = string.Empty;
    public int ReferenceId { get; set; }
    public string ContentType { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
}

public class PresignedMediaUploadDto : IDto
{
    public string UploadUrl { get; set; } = string.Empty;
    public string PublicUrl { get; set; } = string.Empty;
    public string ObjectKey { get; set; } = string.Empty;
}

public class ConfirmMediaUploadRequest : IDto
{
    public string Context { get; set; } = string.Empty;
    public int ReferenceId { get; set; }
    public string ObjectKey { get; set; } = string.Empty;
    public bool? IsPrimary { get; set; }
    public int? SortOrder { get; set; }
}

public class ConfirmMediaUploadDto : IDto
{
    public int? ImageId { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public string ObjectKey { get; set; } = string.Empty;
    public bool? IsPrimary { get; set; }
    public int? SortOrder { get; set; }
}

public class ReorderProductImageItemRequest : IDto
{
    public int ImageId { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsPrimary { get; set; }
}

public class ReorderProductImagesRequest : IDto
{
    public List<ReorderProductImageItemRequest> ImageOrders { get; set; } = new();
}
