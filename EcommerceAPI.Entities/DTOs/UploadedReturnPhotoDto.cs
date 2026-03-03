using EcommerceAPI.Core.Entities;

namespace EcommerceAPI.Entities.DTOs;

public class UploadedReturnPhotoDto : IDto
{
    public string UploadKey { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
}
