using EcommerceAPI.Core.Entities;
using EcommerceAPI.Core.Parameters;

namespace EcommerceAPI.Core.Interfaces;

public interface IProductRepository : IRepository<Product>
{
    Task<(IEnumerable<Product> Items, int TotalCount)> GetPagedAsync(ProductFilterParams filter);
    Task<Product?> GetWithCategoryAsync(int id);
    Task<Product?> GetWithInventoryAsync(int id);
    Task<Product?> GetByIdWithDetailsAsync(int id);
}
