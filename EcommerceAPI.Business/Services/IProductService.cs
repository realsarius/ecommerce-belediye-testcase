using EcommerceAPI.Core.DTOs;

namespace EcommerceAPI.Business.Services;

public interface IProductService
{
    Task<PaginatedResponse<ProductDto>> GetProductsAsync(ProductListRequest request);
    Task<ProductDto?> GetProductByIdAsync(int id);
}
