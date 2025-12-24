using EcommerceAPI.Core.Utilities.Results;

namespace EcommerceAPI.Business.Abstract;

public interface IInventoryService
{
    Task<IResult> DecreaseStockAsync(int productId, int quantity, int userId, string reason);
    Task<IResult> IncreaseStockAsync(int productId, int quantity, int userId, string reason);
}

