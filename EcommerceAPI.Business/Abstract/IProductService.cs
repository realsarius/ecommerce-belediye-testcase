using EcommerceAPI.Entities.DTOs;
using EcommerceAPI.Core.Utilities.Results;

namespace EcommerceAPI.Business.Abstract;

public interface IProductService
{
    Task<IDataResult<PaginatedResponse<ProductDto>>> GetProductsAsync(ProductListRequest request);
    Task<IDataResult<ProductDto>> GetProductByIdAsync(int id);
    
    // Admin controls
    Task<IDataResult<ProductDto>> CreateProductAsync(CreateProductRequest request);
    Task<IDataResult<ProductDto>> UpdateProductAsync(int id, UpdateProductRequest request);
    Task<IResult> DeleteProductAsync(int id);
    Task<IResult> UpdateStockAsync(int productId, UpdateStockRequest request, int userId);
}

