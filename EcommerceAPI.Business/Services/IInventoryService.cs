namespace EcommerceAPI.Business.Services;

public interface IInventoryService
{
    Task DecreaseStockAsync(int productId, int quantity);
    Task IncreaseStockAsync(int productId, int quantity);
}
