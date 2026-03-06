using EcommerceAPI.Core.Utilities.Results;

namespace EcommerceAPI.Business.Abstract;

public interface IInventoryService
{
    Task<IResult> DecreaseStockAsync(int productId, int quantity, int userId, string reason);
    Task<IResult> IncreaseStockAsync(int productId, int quantity, int userId, string reason);
    Task<IResult> ReserveStocksAsync(Dictionary<int, int> productQuantities, int userId, string reason);
    Task ReleaseStocksAsync(Dictionary<int, int> productQuantities, int userId, string reason);
    Task<IResult> BulkAdjustStocksAsync(Dictionary<int, int> quantityChanges, int userId, string reason);
}

