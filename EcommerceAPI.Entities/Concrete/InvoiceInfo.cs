using EcommerceAPI.Entities.Enums;

namespace EcommerceAPI.Entities.Concrete;

public class InvoiceInfo : BaseEntity
{
    public int OrderId { get; set; }
    public InvoiceType Type { get; set; } = InvoiceType.Individual;
    public string FullName { get; set; } = string.Empty;
    public string? TcKimlikNo { get; set; }
    public string? CompanyName { get; set; }
    public string? TaxOffice { get; set; }
    public string? TaxNumber { get; set; }
    public string InvoiceAddress { get; set; } = string.Empty;

    public Order Order { get; set; } = null!;
}
