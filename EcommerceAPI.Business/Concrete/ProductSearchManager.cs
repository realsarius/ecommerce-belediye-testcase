using EcommerceAPI.Business.Abstract;
using EcommerceAPI.Core.Utilities.Results;
using EcommerceAPI.Entities.DTOs;

namespace EcommerceAPI.Business.Concrete;

public class ProductSearchManager : IProductSearchService
{
    private readonly IProductSearchIndexService _productSearchIndexService;

    public ProductSearchManager(IProductSearchIndexService productSearchIndexService)
    {
        _productSearchIndexService = productSearchIndexService;
    }

    public async Task<IDataResult<PaginatedResponse<ProductDto>>> SearchProductsAsync(ProductListRequest request)
    {
        var result = await _productSearchIndexService.SearchAsync(request);
        return new SuccessDataResult<PaginatedResponse<ProductDto>>(result);
    }

    public async Task<IDataResult<List<ProductDto>>> SuggestProductsAsync(string query, int limit = 8)
    {
        if (string.IsNullOrWhiteSpace(query) || query.Trim().Length < 2)
        {
            return new SuccessDataResult<List<ProductDto>>(new List<ProductDto>());
        }

        var normalizedLimit = Math.Clamp(limit, 1, 20);
        var suggestions = await _productSearchIndexService.SuggestAsync(query.Trim(), normalizedLimit);
        return new SuccessDataResult<List<ProductDto>>(suggestions);
    }
}
