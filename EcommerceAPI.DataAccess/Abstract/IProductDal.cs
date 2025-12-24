using System.Linq.Expressions;
using EcommerceAPI.Core.DataAccess;
using EcommerceAPI.Entities.Concrete;

namespace EcommerceAPI.DataAccess.Abstract;

public interface IProductDal : IEntityRepository<Product>
{
    Task<(IEnumerable<Product> Items, int TotalCount)> GetPagedAsync(
        int page, int pageSize, int? categoryId = null, decimal? minPrice = null, 
        decimal? maxPrice = null, string? search = null, bool? inStock = null, 
        string? sortBy = null, bool sortDescending = false);
    Task<Product?> GetWithCategoryAsync(int id);
    Task<Product?> GetWithInventoryAsync(int id);
    Task<Product?> GetByIdWithDetailsAsync(int id);
}
