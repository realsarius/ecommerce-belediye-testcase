using EcommerceAPI.Core.Entities;
using EcommerceAPI.Entities.Enums;

namespace EcommerceAPI.Entities.DTOs;

public class InvoiceInfoDto : IDto
{
    public InvoiceType Type { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? TcKimlikNo { get; set; }
    public string? CompanyName { get; set; }
    public string? TaxOffice { get; set; }
    public string? TaxNumber { get; set; }
    public string InvoiceAddress { get; set; } = string.Empty;
}
