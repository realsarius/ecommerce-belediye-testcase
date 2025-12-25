namespace EcommerceAPI.Entities.Concrete;

public class ShippingAddress : BaseEntity
{
    public int UserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string District { get; set; } = string.Empty;
    public string AddressLine { get; set; } = string.Empty;
    public string? PostalCode { get; set; }
    public bool IsDefault { get; set; } = false;
    

    public User User { get; set; } = null!;
}
