using EcommerceAPI.Core.Entities;

namespace EcommerceAPI.Entities.Concrete;

public class Inventory : IEntity
{
    public int ProductId { get; set; } // Primary Key and foreign key
    public int QuantityAvailable { get; set; }
    public int QuantityReserved { get; set; }
    
    public uint RowVersion { get; set; }
    
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    public Product Product { get; set; } = null!;
}
