using EcommerceAPI.Core.DTOs;

namespace EcommerceAPI.Business.Services;

public interface IProductService
{
    Task<PaginatedResponse<ProductDto>> GetProductsAsync(ProductListRequest request);
    Task<ProductDto?> GetProductByIdAsync(int id);
    
    // Admin controls
    Task<ProductDto> CreateProductAsync(CreateProductRequest request);
    Task<ProductDto?> UpdateProductAsync(int id, UpdateProductRequest request);
    Task<bool> DeleteProductAsync(int id);
    Task<bool> UpdateStockAsync(int productId, UpdateStockRequest request, int userId);
}
