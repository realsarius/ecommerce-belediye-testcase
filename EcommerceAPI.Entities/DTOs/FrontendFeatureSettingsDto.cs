using EcommerceAPI.Core.Entities;

namespace EcommerceAPI.Entities.DTOs;

public class FrontendFeatureSettingsDto : IDto
{
    public bool EnableCheckoutLegalConsents { get; set; }
    public bool EnableCheckoutInvoiceInfo { get; set; }
    public bool EnableShipmentTimeline { get; set; }
    public bool EnableReturnAttachments { get; set; }
}
