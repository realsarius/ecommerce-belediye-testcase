namespace EcommerceAPI.Entities.DTOs;

public sealed class ReturnAttachmentUploadContent
{
    public Stream Content { get; set; } = Stream.Null;
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
}
