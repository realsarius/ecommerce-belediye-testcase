using EcommerceAPI.Entities.Enums;

namespace EcommerceAPI.Entities.Concrete;

public class Order : BaseEntity
{
    public string OrderNumber { get; set; } = string.Empty; 
    public int UserId { get; set; }
    public OrderStatus Status { get; set; } = OrderStatus.PendingPayment;
    public decimal TotalAmount { get; set; }
    public decimal SubtotalAmount { get; set; }
    public string Currency { get; set; } = "TRY";
    public string ShippingAddress { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public string? CargoCompany { get; set; }
    public string? TrackingCode { get; set; }
    public DateTime? ShippedAt { get; set; }
    public DateTime? EstimatedDeliveryDate { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public ShipmentStatus ShipmentStatus { get; set; } = ShipmentStatus.Pending;
    public DateTime? CancelledAt { get; set; }
    public DateTime? PreliminaryInfoAcceptedAt { get; set; }
    public DateTime? DistanceSalesContractAcceptedAt { get; set; }
    public string? AcceptedFromIp { get; set; }
    
    public int? CouponId { get; set; }
    public string? CouponCode { get; set; }
    public decimal DiscountAmount { get; set; }
    public int LoyaltyPointsUsed { get; set; }
    public int LoyaltyPointsEarned { get; set; }
    public decimal LoyaltyDiscountAmount { get; set; }
    public int? GiftCardId { get; set; }
    public string? GiftCardCode { get; set; }
    public decimal GiftCardAmount { get; set; }
    
    public User User { get; set; } = null!;
    public Coupon? Coupon { get; set; }
    public GiftCard? GiftCard { get; set; }
    public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    public ICollection<LoyaltyTransaction> LoyaltyTransactions { get; set; } = new List<LoyaltyTransaction>();
    public ICollection<ReferralTransaction> ReferralTransactions { get; set; } = new List<ReferralTransaction>();
    public Payment? Payment { get; set; }
    public InvoiceInfo? InvoiceInfo { get; set; }
}
