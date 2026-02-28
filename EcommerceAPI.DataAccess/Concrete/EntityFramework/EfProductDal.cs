using EcommerceAPI.Core.DataAccess.EntityFramework;
using EcommerceAPI.DataAccess.Abstract;
using EcommerceAPI.Entities.Concrete;
using Microsoft.EntityFrameworkCore;

using EcommerceAPI.DataAccess.Concrete.EntityFramework.Contexts;

namespace EcommerceAPI.DataAccess.Concrete.EntityFramework;

public class EfProductDal : EfEntityRepositoryBase<Product, AppDbContext>, IProductDal
{
    public EfProductDal(AppDbContext context) : base(context) { }

    public async Task<(IEnumerable<Product> Items, int TotalCount)> GetPagedAsync(
        int page, int pageSize, int? categoryId = null, decimal? minPrice = null,
        decimal? maxPrice = null, string? search = null, bool? inStock = null,
        string? sortBy = null, bool sortDescending = false)
    {
        var query = _dbSet.Include(p => p.Category)
                          .Include(p => p.Inventory)
                          .AsNoTracking()
                          .AsQueryable();

        query = query.Where(p => p.IsActive);

        if (categoryId.HasValue)
            query = query.Where(p => p.CategoryId == categoryId);

        if (minPrice.HasValue)
            query = query.Where(p => p.Price >= minPrice);

        if (maxPrice.HasValue)
            query = query.Where(p => p.Price <= maxPrice);

        if (inStock == true)
            query = query.Where(p => p.Inventory != null && p.Inventory.QuantityAvailable > 0);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchTerm = search.Trim().ToLower();
            query = query.Where(p => p.Name.ToLower().Contains(searchTerm)
                                  || p.SKU.ToLower().Contains(searchTerm));
        }

        var totalCount = await query.CountAsync();

        query = sortBy?.ToLower() switch
        {
            "price" => sortDescending ? query.OrderByDescending(p => p.Price) : query.OrderBy(p => p.Price),
            "created" => sortDescending ? query.OrderByDescending(p => p.CreatedAt) : query.OrderBy(p => p.CreatedAt),
            "wishlistcount" => sortDescending ? query.OrderByDescending(p => p.WishlistCount) : query.OrderBy(p => p.WishlistCount),
            "name" => sortDescending ? query.OrderByDescending(p => p.Name) : query.OrderBy(p => p.Name),
            _ => query.OrderByDescending(p => p.CreatedAt)
        };

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    public async Task<Product?> GetWithCategoryAsync(int id)
    {
        return await _dbSet.Include(p => p.Category).AsNoTracking().FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<Product?> GetWithInventoryAsync(int id)
    {
        return await _dbSet.Include(p => p.Inventory).AsNoTracking().FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<List<Product>> GetByIdsWithInventoryAsync(List<int> ids)
    {
        return await _dbSet.Include(p => p.Inventory)
                           .Where(p => ids.Contains(p.Id))
                           .AsNoTracking()
                           .ToListAsync();
    }

    public async Task<Product?> GetByIdWithDetailsAsync(int id)
    {
        return await _dbSet
            .Include(p => p.Category)
            .Include(p => p.Inventory)
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<(IEnumerable<Product> Items, int TotalCount)> GetPagedForSellerAsync(
        int page, int pageSize, int sellerId, int? categoryId = null, decimal? minPrice = null,
        decimal? maxPrice = null, string? search = null, string? sortBy = null, bool sortDescending = false)
    {
        var query = _dbSet.Include(p => p.Category)
                          .Include(p => p.Inventory)
                          .Include(p => p.Seller)
                          .Where(p => p.SellerId == sellerId)
                          .AsNoTracking()
                          .AsQueryable();

        if (categoryId.HasValue)
            query = query.Where(p => p.CategoryId == categoryId);

        if (minPrice.HasValue)
            query = query.Where(p => p.Price >= minPrice);

        if (maxPrice.HasValue)
            query = query.Where(p => p.Price <= maxPrice);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchTerm = search.Trim().ToLower();
            query = query.Where(p => p.Name.ToLower().Contains(searchTerm) 
                                  || p.Description.ToLower().Contains(searchTerm));
        }

        var totalCount = await query.CountAsync();

        query = sortBy?.ToLower() switch
        {
            "price" => sortDescending ? query.OrderByDescending(p => p.Price) : query.OrderBy(p => p.Price),
            "name" => sortDescending ? query.OrderByDescending(p => p.Name) : query.OrderBy(p => p.Name),
            _ => sortDescending ? query.OrderByDescending(p => p.CreatedAt) : query.OrderBy(p => p.CreatedAt)
        };

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    public async Task<List<Product>> GetAllActiveWithDetailsAsync()
    {
        return await _dbSet
            .Include(p => p.Category)
            .Include(p => p.Inventory)
            .Include(p => p.Seller)
            .Where(p => p.IsActive)
            .AsNoTracking()
            .ToListAsync();
    }

}
