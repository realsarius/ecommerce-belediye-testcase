namespace EcommerceAPI.Infrastructure.Settings;

public class ReturnAttachmentSettings
{
    public string RootPath { get; set; } = "private-uploads/returns";
    public int MaxFiles { get; set; } = 5;
    public long MaxFileSizeBytes { get; set; } = 5 * 1024 * 1024;
    public List<string> AllowedContentTypes { get; set; } =
    [
        "image/jpeg",
        "image/png",
        "image/webp",
        "image/heic"
    ];
}
