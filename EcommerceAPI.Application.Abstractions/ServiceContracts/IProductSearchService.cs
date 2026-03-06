using EcommerceAPI.Core.Utilities.Results;
using EcommerceAPI.Entities.DTOs;

namespace EcommerceAPI.Application.Abstractions.ServiceContracts;

public interface IProductSearchService
{
    Task<IDataResult<PaginatedResponse<ProductDto>>> SearchProductsAsync(ProductListRequest request);
    Task<IDataResult<List<ProductDto>>> SuggestProductsAsync(string query, int limit = 8);
}
