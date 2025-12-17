using System.ComponentModel.DataAnnotations;

namespace EcommerceAPI.Core.Entities;

public class Inventory
{
    public int ProductId { get; set; } // Primary Key and foreign key
    public int QuantityAvailable { get; set; }
    public int QuantityReserved { get; set; }
    
    [Timestamp] // Optimistic concurrency token
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    
    // Navigation property
    public Product Product { get; set; } = null!;
}
