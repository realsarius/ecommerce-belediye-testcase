namespace EcommerceAPI.Entities.Concrete;

public class ReturnRequestAttachment : BaseEntity
{
    public int ReturnRequestId { get; set; }
    public string OriginalFileName { get; set; } = string.Empty;
    public string StoredFileName { get; set; } = string.Empty;
    public string RelativePath { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long SizeBytes { get; set; }

    public ReturnRequest ReturnRequest { get; set; } = null!;
}
