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
}
