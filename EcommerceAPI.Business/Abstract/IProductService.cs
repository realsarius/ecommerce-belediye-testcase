using EcommerceAPI.Entities.DTOs;
using EcommerceAPI.Core.Utilities.Results;

namespace EcommerceAPI.Business.Abstract;

public interface IProductService
{
    Task<IDataResult<PaginatedResponse<ProductDto>>> GetProductsAsync(ProductListRequest request);
    Task<IDataResult<PaginatedResponse<ProductDto>>> GetProductsForSellerAsync(ProductListRequest request, int sellerId);
    Task<IDataResult<ProductDto>> GetProductByIdAsync(int id);
    

    Task<IDataResult<ProductDto>> CreateProductAsync(CreateProductRequest request, int? sellerId = null);
    Task<IDataResult<ProductDto>> UpdateProductAsync(int id, UpdateProductRequest request, int? sellerId = null);
    Task<IResult> DeleteProductAsync(int id, int? sellerId = null);
    Task<IResult> UpdateStockAsync(int productId, UpdateStockRequest request, int userId);
    Task<bool> IsProductOwnedBySellerAsync(int productId, int sellerId);
}

