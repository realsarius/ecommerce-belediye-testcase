using EcommerceAPI.Core.Utilities.Results;
using EcommerceAPI.Entities.DTOs;

namespace EcommerceAPI.Business.Abstract;

public interface IProductSearchService
{
    Task<IDataResult<PaginatedResponse<ProductDto>>> SearchProductsAsync(ProductListRequest request);
}
