namespace EcommerceAPI.Business.Services.Abstract;

public interface IInventoryService
{
    Task DecreaseStockAsync(int productId, int quantity, int userId, string reason);
    Task IncreaseStockAsync(int productId, int quantity, int userId, string reason);
}
