namespace EcommerceAPI.Entities.Settings;

public class FrontendFeatureSettings
{
    public bool EnableCheckoutLegalConsents { get; set; } = true;
    public bool EnableCheckoutInvoiceInfo { get; set; } = true;
    public bool EnableShipmentTimeline { get; set; } = true;
    public bool EnableReturnAttachments { get; set; } = true;
    public bool EnableAdminProductImageUploader { get; set; } = true;
}
