using EcommerceAPI.Business.Abstract;
using EcommerceAPI.Entities.DTOs;
using EcommerceAPI.Core.Utilities.Results;
using EcommerceAPI.DataAccess.Abstract;
using EcommerceAPI.Core.Parameters;
using EcommerceAPI.Entities.Concrete;
using EcommerceAPI.Core.Interfaces;
using EcommerceAPI.DataAccess.Concrete.EntityFramework.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace EcommerceAPI.Business.Concrete;

public class ProductManager : IProductService
{
    private readonly IProductDal _productDal;
    private readonly IInventoryDal _inventoryDal;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICacheService _cacheService;
    private readonly AppDbContext _context;
    private readonly ILogger<ProductManager> _logger;
    private readonly int _productListCacheTTL;

    public ProductManager(
        IProductDal productDal,
        IInventoryDal inventoryDal,
        IUnitOfWork unitOfWork,
        ICacheService cacheService,
        AppDbContext context,
        ILogger<ProductManager> logger,
        IConfiguration configuration)
    {
        _productDal = productDal;
        _inventoryDal = inventoryDal;
        _unitOfWork = unitOfWork;
        _cacheService = cacheService;
        _context = context;
        _logger = logger;
        _productListCacheTTL = configuration.GetValue<int>("Cache:ProductListTTLMinutes", 5);
    }

    public async Task<IDataResult<PaginatedResponse<ProductDto>>> GetProductsAsync(ProductListRequest request)
    {
        var cacheKey = BuildProductListCacheKey(request);

        // Cache'den kontrol et
        var cachedResult = await _cacheService.GetAsync<PaginatedResponse<ProductDto>>(cacheKey);
        if (cachedResult != null)
        {
            _logger.LogDebug("Cache HIT: {CacheKey}", cacheKey);
            return new SuccessDataResult<PaginatedResponse<ProductDto>>(cachedResult);
        }

        _logger.LogDebug("Cache MISS: {CacheKey}", cacheKey);

        var (items, totalCount) = await _productDal.GetPagedAsync(
            request.Page,
            request.PageSize,
            request.CategoryId,
            request.MinPrice,
            request.MaxPrice,
            request.Search,
            request.InStock,
            request.SortBy,
            request.SortDescending
        );
        var productDtos = items.Select(MapToDto).ToList();

        var result = new PaginatedResponse<ProductDto>
        {
            Items = productDtos,
            Page = request.Page,
            PageSize = request.PageSize,
            TotalCount = totalCount
        };

        await _cacheService.SetAsync(cacheKey, result, TimeSpan.FromMinutes(_productListCacheTTL));

        return new SuccessDataResult<PaginatedResponse<ProductDto>>(result);
    }

    public async Task<IDataResult<PaginatedResponse<ProductDto>>> GetProductsForSellerAsync(ProductListRequest request, int sellerId)
    {
        // Seller'ın kendi ürünlerini filtrele (cache'siz - seller admin paneli için)
        var query = _context.Products
            .Include(p => p.Category)
            .Include(p => p.Inventory)
            .Include(p => p.Seller)
            .Where(p => p.SellerId == sellerId);

        // Opsiyonel filtreler
        if (request.CategoryId.HasValue)
            query = query.Where(p => p.CategoryId == request.CategoryId.Value);

        if (!string.IsNullOrEmpty(request.Search))
            query = query.Where(p => p.Name.Contains(request.Search) || p.Description.Contains(request.Search));

        if (request.MinPrice.HasValue)
            query = query.Where(p => p.Price >= request.MinPrice.Value);

        if (request.MaxPrice.HasValue)
            query = query.Where(p => p.Price <= request.MaxPrice.Value);

        // Sıralama
        query = request.SortBy?.ToLower() switch
        {
            "price" => request.SortDescending ? query.OrderByDescending(p => p.Price) : query.OrderBy(p => p.Price),
            "name" => request.SortDescending ? query.OrderByDescending(p => p.Name) : query.OrderBy(p => p.Name),
            _ => request.SortDescending ? query.OrderByDescending(p => p.CreatedAt) : query.OrderBy(p => p.CreatedAt)
        };

        var totalCount = await query.CountAsync();
        var items = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync();

        var result = new PaginatedResponse<ProductDto>
        {
            Items = items.Select(MapToDto).ToList(),
            Page = request.Page,
            PageSize = request.PageSize,
            TotalCount = totalCount
        };

        return new SuccessDataResult<PaginatedResponse<ProductDto>>(result);
    }

    public async Task<IDataResult<ProductDto>> GetProductByIdAsync(int id)
    {
        var product = await _productDal.GetByIdWithDetailsAsync(id);
        
        if (product == null || !product.IsActive)
            return new ErrorDataResult<ProductDto>("Product not found");

        return new SuccessDataResult<ProductDto>(MapToDto(product));
    }

    public async Task<IDataResult<ProductDto>> CreateProductAsync(CreateProductRequest request, int? sellerId = null)
    {
        var product = new Product
        {
            Name = request.Name,
            Description = request.Description,
            Price = request.Price,
            SKU = request.SKU,
            CategoryId = request.CategoryId,
            SellerId = sellerId, // Seller eklediğinde kendi ID'si atanır, Admin için null
            IsActive = true,
            Currency = "TRY"
        };

        await _productDal.AddAsync(product);

        var inventory = new Inventory
        {
            Product = product,
            QuantityAvailable = request.InitialStock,
            QuantityReserved = 0
        };

        await _inventoryDal.AddAsync(inventory);

        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Product created: {ProductId} by Seller: {SellerId}", product.Id, sellerId?.ToString() ?? "Admin");

        product.Inventory = inventory;
        return new SuccessDataResult<ProductDto>(MapToDto(product), "Ürün başarıyla oluşturuldu");
    }

    public async Task<IDataResult<ProductDto>> UpdateProductAsync(int id, UpdateProductRequest request, int? sellerId = null)
    {
        var product = await _productDal.GetAsync(p => p.Id == id);
        
        if (product == null)
            return new ErrorDataResult<ProductDto>("Ürün bulunamadı");

        // Seller ise sadece kendi ürününü güncelleyebilir
        if (sellerId.HasValue && product.SellerId != sellerId)
            return new ErrorDataResult<ProductDto>("Bu ürünü düzenleme yetkiniz yok");

        product.Name = request.Name;
        product.Description = request.Description;
        product.Price = request.Price;
        product.CategoryId = request.CategoryId;
        product.IsActive = request.IsActive;
        product.UpdatedAt = DateTime.UtcNow;

        _productDal.Update(product);
        await _unitOfWork.SaveChangesAsync();

        var updatedProduct = await _productDal.GetByIdWithDetailsAsync(id);
        return new SuccessDataResult<ProductDto>(MapToDto(updatedProduct!), "Ürün başarıyla güncellendi");
    }

    public async Task<IResult> DeleteProductAsync(int id, int? sellerId = null)
    {
        var product = await _productDal.GetAsync(p => p.Id == id);
        
        if (product == null)
            return new ErrorResult("Ürün bulunamadı");

        // Seller ise sadece kendi ürününü silebilir
        if (sellerId.HasValue && product.SellerId != sellerId)
            return new ErrorResult("Bu ürünü silme yetkiniz yok");

        product.IsActive = false;
        product.UpdatedAt = DateTime.UtcNow;

        _productDal.Update(product);
        await _unitOfWork.SaveChangesAsync();

        return new SuccessResult("Ürün başarıyla silindi");
    }

    public async Task<IResult> UpdateStockAsync(int productId, UpdateStockRequest request, int userId)
    {
        var inventory = await _inventoryDal.GetByProductIdAsync(productId);

        if (inventory == null)
            return new ErrorResult("Stok bulunamadı");

        var newQuantity = inventory.QuantityAvailable + request.Delta;
        if (newQuantity < 0)
            return new ErrorResult("Yetersiz stok");

        inventory.QuantityAvailable = newQuantity;
        _inventoryDal.Update(inventory);

        var movement = new InventoryMovement
        {
            ProductId = productId,
            UserId = userId,
            Delta = request.Delta,
            Reason = request.Reason,
            Notes = request.Notes
        };

        await _inventoryDal.AddMovementAsync(movement);
        await _unitOfWork.SaveChangesAsync();

        return new SuccessResult("Stok başarıyla güncellendi");
    }

    public async Task<bool> IsProductOwnedBySellerAsync(int productId, int sellerId)
    {
        var product = await _productDal.GetAsync(p => p.Id == productId);
        return product?.SellerId == sellerId;
    }

    private static string BuildProductListCacheKey(ProductListRequest request)
    {
        return $"products:{request.Page}:{request.PageSize}:{request.CategoryId}:{request.MinPrice}:{request.MaxPrice}:{request.Search}:{request.InStock}:{request.SortBy}:{request.SortDescending}";
    }

    private static ProductDto MapToDto(Product p)
    {
        return new ProductDto
        {
            Id = p.Id,
            Name = p.Name,
            Description = p.Description,
            Price = p.Price,
            Currency = p.Currency,
            SKU = p.SKU,
            IsActive = p.IsActive,
            CategoryId = p.CategoryId,
            CategoryName = p.Category?.Name ?? string.Empty,
            StockQuantity = p.Inventory?.QuantityAvailable ?? 0,
            SellerId = p.SellerId,
            SellerBrandName = p.Seller?.BrandName
        };
    }
}
