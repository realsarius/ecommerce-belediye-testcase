using EcommerceAPI.Business.Services.Abstract;
using EcommerceAPI.Core.DTOs;
using EcommerceAPI.Core.Entities;
using EcommerceAPI.Core.Interfaces;
using EcommerceAPI.Core.Parameters;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace EcommerceAPI.Business.Services.Concrete;

public class ProductService : IProductService
{
    private readonly IProductRepository _productRepository;
    private readonly IInventoryRepository _inventoryRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICacheService _cacheService;
    private readonly ILogger<ProductService> _logger;
    private readonly int _productListCacheTTL;

    public ProductService(
        IProductRepository productRepository,
        IInventoryRepository inventoryRepository,
        IUnitOfWork unitOfWork,
        ICacheService cacheService,
        ILogger<ProductService> logger,
        IConfiguration configuration)
    {
        _productRepository = productRepository;
        _inventoryRepository = inventoryRepository;
        _unitOfWork = unitOfWork;
        _cacheService = cacheService;
        _logger = logger;
        _productListCacheTTL = configuration.GetValue<int>("Cache:ProductListTTLMinutes", 5);
    }

    public async Task<PaginatedResponse<ProductDto>> GetProductsAsync(ProductListRequest request)
    {
        var cacheKey = BuildProductListCacheKey(request);

        // Cache'den kontrol et
        var cachedResult = await _cacheService.GetAsync<PaginatedResponse<ProductDto>>(cacheKey);
        if (cachedResult != null)
        {
            _logger.LogDebug("Cache HIT: {CacheKey}", cacheKey);
            return cachedResult;
        }

        _logger.LogDebug("Cache MISS: {CacheKey}", cacheKey);

        var filter = new ProductFilterParams(
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
        
        var (items, totalCount) = await _productRepository.GetPagedAsync(filter);
        var productDtos = items.Select(MapToDto).ToList();

        var result = new PaginatedResponse<ProductDto>
        {
            Items = productDtos,
            Page = request.Page,
            PageSize = request.PageSize,
            TotalCount = totalCount
        };

        await _cacheService.SetAsync(cacheKey, result, TimeSpan.FromMinutes(_productListCacheTTL));

        return result;
    }

    public async Task<ProductDto?> GetProductByIdAsync(int id)
    {
        var product = await _productRepository.GetByIdWithDetailsAsync(id);
        
        if (product == null || !product.IsActive)
            return null;

        return MapToDto(product);
    }

    public async Task<ProductDto> CreateProductAsync(CreateProductRequest request)
    {
        var product = new Product
        {
            Name = request.Name,
            Description = request.Description,
            Price = request.Price,
            SKU = request.SKU,
            CategoryId = request.CategoryId,
            IsActive = true,
            Currency = "TRY"
        };

        await _productRepository.AddAsync(product);

        var inventory = new Inventory
        {
            Product = product,
            QuantityAvailable = request.InitialStock,
            QuantityReserved = 0
        };

        await _inventoryRepository.AddAsync(inventory);

        await _unitOfWork.SaveChangesAsync();

        product.Inventory = inventory;
        return MapToDto(product);
    }

    public async Task<ProductDto?> UpdateProductAsync(int id, UpdateProductRequest request)
    {
        var product = await _productRepository.GetByIdAsync(id);
        
        if (product == null)
            return null;

        product.Name = request.Name;
        product.Description = request.Description;
        product.Price = request.Price;
        product.CategoryId = request.CategoryId;
        product.IsActive = request.IsActive;
        product.UpdatedAt = DateTime.UtcNow;

        _productRepository.Update(product);
        await _unitOfWork.SaveChangesAsync();

        var updatedProduct = await _productRepository.GetByIdWithDetailsAsync(id);
        return updatedProduct != null ? MapToDto(updatedProduct) : null;
    }

    public async Task<bool> DeleteProductAsync(int id)
    {
        var product = await _productRepository.GetByIdAsync(id);
        
        if (product == null)
            return false;

        product.IsActive = false;
        product.UpdatedAt = DateTime.UtcNow;

        _productRepository.Update(product);
        await _unitOfWork.SaveChangesAsync();

        return true;
    }

    public async Task<bool> UpdateStockAsync(int productId, UpdateStockRequest request, int userId)
    {
        var inventory = await _inventoryRepository.GetByProductIdAsync(productId);

        if (inventory == null)
            return false;

        var newQuantity = inventory.QuantityAvailable + request.Delta;
        if (newQuantity < 0)
            return false;

        inventory.QuantityAvailable = newQuantity;
        _inventoryRepository.Update(inventory);

        var movement = new InventoryMovement
        {
            ProductId = productId,
            UserId = userId,
            Delta = request.Delta,
            Reason = request.Reason,
            Notes = request.Notes
        };

        await _inventoryRepository.AddMovementAsync(movement);
        await _unitOfWork.SaveChangesAsync();

        return true;
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
            StockQuantity = p.Inventory?.QuantityAvailable ?? 0
        };
    }
}
