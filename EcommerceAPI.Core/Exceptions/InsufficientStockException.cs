namespace EcommerceAPI.Core.Exceptions;

public class InsufficientStockException : DomainException
{
    public int ProductId { get; }
    public int RequestedQuantity { get; }
    public int AvailableQuantity { get; }

    public InsufficientStockException(int productId, int requestedQuantity, int availableQuantity)
        : base($"Yetersiz stok. Mevcut: {availableQuantity}, İstenen: {requestedQuantity}", "INSUFFICIENT_STOCK")
    {
        ProductId = productId;
        RequestedQuantity = requestedQuantity;
        AvailableQuantity = availableQuantity;
    }
}
