using EcommerceAPI.Core.Entities;
using EcommerceAPI.Core.Interfaces;
using EcommerceAPI.Core.Parameters;
using Microsoft.EntityFrameworkCore;

namespace EcommerceAPI.Data.Repositories;

public class ProductRepository : GenericRepository<Product>, IProductRepository
{
    public ProductRepository(AppDbContext context) : base(context) { }

    public async Task<(IEnumerable<Product> Items, int TotalCount)> GetPagedAsync(ProductFilterParams filter)
    {
        var query = _dbSet.Include(p => p.Category)
                          .Include(p => p.Inventory)
                          .AsNoTracking()
                          .AsQueryable();

        query = query.Where(p => p.IsActive);

        if (filter.CategoryId.HasValue)
            query = query.Where(p => p.CategoryId == filter.CategoryId);

        if (filter.MinPrice.HasValue)
            query = query.Where(p => p.Price >= filter.MinPrice);

        if (filter.MaxPrice.HasValue)
            query = query.Where(p => p.Price <= filter.MaxPrice);

        if (filter.InStock == true)
            query = query.Where(p => p.Inventory != null && p.Inventory.QuantityAvailable > 0);

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var searchTerm = filter.Search.Trim().ToLower();
            query = query.Where(p => p.Name.ToLower().Contains(searchTerm) 
                                  || p.SKU.ToLower().Contains(searchTerm));
        }

        var totalCount = await query.CountAsync();

        query = filter.SortBy?.ToLower() switch
        {
            "price" => filter.SortDescending ? query.OrderByDescending(p => p.Price) : query.OrderBy(p => p.Price),
            "created" => filter.SortDescending ? query.OrderByDescending(p => p.CreatedAt) : query.OrderBy(p => p.CreatedAt),
            "name" => filter.SortDescending ? query.OrderByDescending(p => p.Name) : query.OrderBy(p => p.Name),
            _ => query.OrderByDescending(p => p.CreatedAt)
        };

        var items = await query
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
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

    public async Task<Product?> GetByIdWithDetailsAsync(int id)
    {
        return await _dbSet
            .Include(p => p.Category)
            .Include(p => p.Inventory)
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id);
    }
}
