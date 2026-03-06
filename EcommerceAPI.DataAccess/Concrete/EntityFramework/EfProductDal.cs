using EcommerceAPI.Core.DataAccess.EntityFramework;
using EcommerceAPI.DataAccess.Abstract;
using EcommerceAPI.Entities.Concrete;
using EcommerceAPI.Entities.DTOs;
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
                          .Include(p => p.Images)
                          .Include(p => p.Variants)
                          .Include(p => p.CampaignProducts)
                          .ThenInclude(cp => cp.Campaign)
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
                           .Include(p => p.Images)
                           .Include(p => p.Variants)
                           .Include(p => p.CampaignProducts)
                           .ThenInclude(cp => cp.Campaign)
                           .Include(p => p.Category)
                           .Where(p => ids.Contains(p.Id))
                           .AsNoTracking()
                           .ToListAsync();
    }

    public async Task<Product?> GetByIdWithDetailsAsync(int id)
    {
        return await _dbSet
            .Include(p => p.Category)
            .Include(p => p.Inventory)
            .Include(p => p.Images)
            .Include(p => p.Variants)
            .Include(p => p.CampaignProducts)
            .ThenInclude(cp => cp.Campaign)
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<Product?> GetByIdForUpdateAsync(int id)
    {
        return await _dbSet
            .Include(p => p.Inventory)
            .Include(p => p.Images)
            .Include(p => p.Variants)
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<Product?> GetByImageIdForUpdateAsync(int imageId)
    {
        return await _dbSet
            .Include(p => p.Images)
            .FirstOrDefaultAsync(p => p.Images.Any(image => image.Id == imageId));
    }

    public async Task<(IEnumerable<Product> Items, int TotalCount)> GetPagedForSellerAsync(
        int page, int pageSize, int sellerId, int? categoryId = null, decimal? minPrice = null,
        decimal? maxPrice = null, string? search = null, string? sortBy = null, bool sortDescending = false)
    {
        var query = _dbSet.Include(p => p.Category)
                          .Include(p => p.Inventory)
                          .Include(p => p.Seller)
                          .Include(p => p.Images)
                          .Include(p => p.Variants)
                          .Include(p => p.CampaignProducts)
                          .ThenInclude(cp => cp.Campaign)
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
            .Include(p => p.Images)
            .Include(p => p.Variants)
            .Include(p => p.CampaignProducts)
            .ThenInclude(cp => cp.Campaign)
            .Where(p => p.IsActive)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<IReadOnlyList<string>> GetAllImageObjectKeysAsync()
    {
        return await _context.Set<ProductImage>()
            .AsNoTracking()
            .Where(image => image.ObjectKey != null && image.ObjectKey != string.Empty)
            .Select(image => image.ObjectKey!)
            .ToListAsync();
    }

    public Task<int> CountProductsWithoutSellerAsync()
    {
        return _dbSet
            .AsNoTracking()
            .CountAsync(product => !product.SellerId.HasValue);
    }

    public async Task<IReadOnlyList<int>> GetProductIdsWithoutSellerAsync()
    {
        return await _dbSet
            .AsNoTracking()
            .Where(product => !product.SellerId.HasValue)
            .OrderBy(product => product.Id)
            .Select(product => product.Id)
            .ToListAsync();
    }

    public Task<int> BackfillMissingSellerIdsAsync(int sellerId, DateTime updatedAtUtc)
    {
        return _dbSet
            .Where(product => !product.SellerId.HasValue)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(product => product.SellerId, sellerId)
                .SetProperty(product => product.UpdatedAt, updatedAtUtc));
    }

    public async Task<(int ActiveProducts, int ActiveSellers, string Currency)> GetAdminDashboardProductSummaryAsync()
    {
        var activeProductsQuery = _dbSet
            .AsNoTracking()
            .Where(product => product.IsActive);

        var activeProducts = await activeProductsQuery.CountAsync();
        var activeSellers = await activeProductsQuery
            .Where(product => product.SellerId.HasValue)
            .Select(product => product.SellerId!.Value)
            .Distinct()
            .CountAsync();
        var currency = await activeProductsQuery
            .Select(product => product.Currency)
            .FirstOrDefaultAsync()
            ?? "TRY";

        return (activeProducts, activeSellers, currency);
    }

    public async Task<IReadOnlyList<AdminDashboardLowStockItemDto>> GetAdminDashboardLowStockAsync(int threshold, int limit = 5)
    {
        return await _dbSet
            .AsNoTracking()
            .Where(product => product.IsActive && product.Inventory != null && product.Inventory.QuantityAvailable <= threshold)
            .OrderBy(product => product.Inventory != null ? product.Inventory.QuantityAvailable : int.MaxValue)
            .ThenBy(product => product.Name)
            .Select(product => new AdminDashboardLowStockItemDto
            {
                ProductId = product.Id,
                Name = product.Name,
                Stock = product.Inventory != null ? product.Inventory.QuantityAvailable : 0,
                SellerName = product.Seller != null ? product.Seller.BrandName : "Satıcı bilgisi yok"
            })
            .Take(Math.Clamp(limit, 1, 20))
            .ToListAsync();
    }

}
