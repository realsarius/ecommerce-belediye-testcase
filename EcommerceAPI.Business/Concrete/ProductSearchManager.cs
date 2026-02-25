using EcommerceAPI.Business.Abstract;
using EcommerceAPI.Business.Extensions;
using EcommerceAPI.Core.Utilities.Results;
using EcommerceAPI.DataAccess.Abstract;
using EcommerceAPI.Entities.DTOs;

namespace EcommerceAPI.Business.Concrete;

public class ProductSearchManager : IProductSearchService
{
    private readonly IProductDal _productDal;

    public ProductSearchManager(IProductDal productDal)
    {
        _productDal = productDal;
    }

    public async Task<IDataResult<PaginatedResponse<ProductDto>>> SearchProductsAsync(ProductListRequest request)
    {
        var (items, totalCount) = await _productDal.GetPagedAsync(
            request.Page,
            request.PageSize,
            request.CategoryId,
            request.MinPrice,
            request.MaxPrice,
            request.Search,
            request.InStock,
            request.SortBy,
            request.SortDescending
        );

        var result = new PaginatedResponse<ProductDto>
        {
            Items = items.Select(p => p.ToDto()).ToList(),
            Page = request.Page,
            PageSize = request.PageSize,
            TotalCount = totalCount
        };

        return new SuccessDataResult<PaginatedResponse<ProductDto>>(result);
    }
}
