using EcommerceAPI.Core.Entities;
using EcommerceAPI.Entities.Enums;

namespace EcommerceAPI.Entities.DTOs;

public class CheckoutInvoiceInfoRequest : IDto
{
    public InvoiceType Type { get; set; } = InvoiceType.Individual;
    public string? FullName { get; set; }
    public string? TcKimlikNo { get; set; }
    public string? CompanyName { get; set; }
    public string? TaxOffice { get; set; }
    public string? TaxNumber { get; set; }
    public string InvoiceAddress { get; set; } = string.Empty;
}
