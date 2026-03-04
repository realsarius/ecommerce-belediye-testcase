using EcommerceAPI.Core.Entities;

namespace EcommerceAPI.Entities.DTOs;

public class ReturnRequestAttachmentDto : IDto
{
    public int Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public DateTime CreatedAt { get; set; }
}
