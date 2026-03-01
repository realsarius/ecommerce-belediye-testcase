using EcommerceAPI.Entities.Concrete;
using EcommerceAPI.Entities.DTOs;

namespace EcommerceAPI.Business.Extensions;

public static class MappingExtensions
{
    public static OrderDto ToDto(this Order order)
    {
        return new OrderDto
        {
            Id = order.Id,
            OrderNumber = order.OrderNumber,
            Status = order.Status.ToString(),
            TotalAmount = order.TotalAmount,
            Currency = order.Currency,
            ShippingAddress = order.ShippingAddress,
            CustomerName = order.User != null ? $"{order.User.FirstName} {order.User.LastName}" : "Bilinmiyor",
            Notes = order.Notes,
            CreatedAt = order.CreatedAt,
            CancelledAt = order.CancelledAt,
            CouponCode = order.CouponCode,
            DiscountAmount = order.DiscountAmount,
            Items = order.OrderItems.Select(oi => new OrderItemDto
            {
                Id = oi.Id,
                ProductId = oi.ProductId,
                ProductName = oi.Product?.Name ?? string.Empty,
                ProductSKU = oi.Product?.SKU ?? string.Empty,
                Quantity = oi.Quantity,
                PriceSnapshot = oi.PriceSnapshot,
                LineTotal = oi.Quantity * oi.PriceSnapshot
            }).ToList(),
            Payment = order.Payment != null ? new PaymentDto
            {
                Id = order.Payment.Id,
                Amount = order.Payment.Amount,
                Currency = order.Payment.Currency,
                Status = order.Payment.Status.ToString(),
                PaymentMethod = order.Payment.PaymentMethod,
                CreatedAt = order.Payment.CreatedAt
            } : null
        };
    }

    public static ProductDto ToDto(this Product p)
    {
        var activeCampaign = p.GetActiveCampaignProduct();

        return new ProductDto
        {
            Id = p.Id,
            Name = p.Name,
            Description = p.Description,
            Price = activeCampaign?.CampaignPrice ?? p.Price,
            Currency = p.Currency,
            SKU = p.SKU,
            IsActive = p.IsActive,
            CategoryId = p.CategoryId,
            CategoryName = p.Category?.Name ?? string.Empty,
            StockQuantity = p.Inventory?.QuantityAvailable ?? 0,
            SellerId = p.SellerId,
            SellerBrandName = p.Seller?.BrandName,
            WishlistCount = p.WishlistCount,
            HasActiveCampaign = activeCampaign != null,
            OriginalPrice = p.Price,
            CampaignPrice = activeCampaign?.CampaignPrice,
            CampaignName = activeCampaign?.Campaign?.Name,
            CampaignBadgeText = activeCampaign?.Campaign?.BadgeText,
            CampaignEndsAt = activeCampaign?.Campaign?.EndsAt,
            IsCampaignFeatured = activeCampaign?.IsFeatured ?? false
        };
    }
}
