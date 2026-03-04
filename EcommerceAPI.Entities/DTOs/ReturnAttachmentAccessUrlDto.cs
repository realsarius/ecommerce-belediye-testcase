using EcommerceAPI.Core.Entities;

namespace EcommerceAPI.Entities.DTOs;

public class ReturnAttachmentAccessUrlDto : IDto
{
    public string Url { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
}
