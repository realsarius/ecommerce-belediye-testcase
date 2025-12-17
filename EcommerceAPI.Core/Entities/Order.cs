using EcommerceAPI.Core.Enums;

namespace EcommerceAPI.Core.Entities;

public class Order : BaseEntity
{
    public string OrderNumber { get; set; } = string.Empty; 
    public int UserId { get; set; }
    public OrderStatus Status { get; set; } = OrderStatus.PendingPayment;
    public decimal TotalAmount { get; set; }
    public string Currency { get; set; } = "TRY";
    public string ShippingAddress { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public DateTime? CancelledAt { get; set; }
    
    // Navigation properties
    public User User { get; set; } = null!;
    public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    public Payment? Payment { get; set; }
}
