using EcommerceAPI.Entities.DTOs;

namespace EcommerceAPI.Business.Abstract;

public interface IProductSearchIndexService
{
    Task<PaginatedResponse<ProductDto>> SearchAsync(ProductListRequest request, CancellationToken cancellationToken = default);
    Task<List<ProductDto>> SuggestAsync(string query, int limit = 8, CancellationToken cancellationToken = default);
    Task IndexProductAsync(int productId, CancellationToken cancellationToken = default);
    Task DeleteProductAsync(int productId, CancellationToken cancellationToken = default);
    Task EnsureIndexAsync(CancellationToken cancellationToken = default);
}
