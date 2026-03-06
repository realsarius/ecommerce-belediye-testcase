using System.Linq.Expressions;
using EcommerceAPI.Core.DataAccess;
using EcommerceAPI.Entities.Concrete;
using EcommerceAPI.Entities.DTOs;

namespace EcommerceAPI.DataAccess.Abstract;

public interface IProductDal : IEntityRepository<Product>
{
    Task<(IEnumerable<Product> Items, int TotalCount)> GetPagedAsync(
        int page, int pageSize, int? categoryId = null, decimal? minPrice = null, 
        decimal? maxPrice = null, string? search = null, bool? inStock = null, 
        string? sortBy = null, bool sortDescending = false);
    Task<Product?> GetWithCategoryAsync(int id);
    Task<Product?> GetWithInventoryAsync(int id);
    Task<List<Product>> GetByIdsWithInventoryAsync(List<int> ids);
    Task<Product?> GetByIdWithDetailsAsync(int id);
    Task<Product?> GetByIdForUpdateAsync(int id);
    Task<Product?> GetByImageIdForUpdateAsync(int imageId);
    Task<(IEnumerable<Product> Items, int TotalCount)> GetPagedForSellerAsync(
        int page, int pageSize, int sellerId, int? categoryId = null, decimal? minPrice = null,
        decimal? maxPrice = null, string? search = null, string? sortBy = null, bool sortDescending = false);
    Task<List<Product>> GetAllActiveWithDetailsAsync();
    Task<IReadOnlyList<string>> GetAllImageObjectKeysAsync();
    Task<int> CountProductsWithoutSellerAsync();
    Task<int> BackfillMissingSellerIdsAsync(int sellerId, DateTime updatedAtUtc);
    Task<(int ActiveProducts, int ActiveSellers, string Currency)> GetAdminDashboardProductSummaryAsync();
    Task<IReadOnlyList<AdminDashboardLowStockItemDto>> GetAdminDashboardLowStockAsync(int threshold, int limit = 5);
}
