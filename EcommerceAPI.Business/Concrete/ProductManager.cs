using EcommerceAPI.Business.Abstract;
using EcommerceAPI.Entities.DTOs;
using EcommerceAPI.Core.Utilities.Results;
using EcommerceAPI.DataAccess.Abstract;
using EcommerceAPI.Core.Parameters;
using EcommerceAPI.Entities.Concrete;
using EcommerceAPI.Core.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace EcommerceAPI.Business.Concrete;

public class ProductManager : IProductService
{
    private readonly IProductDal _productDal;
    private readonly IInventoryDal _inventoryDal;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICacheService _cacheService;
    private readonly ILogger<ProductManager> _logger;
    private readonly int _productListCacheTTL;

    public ProductManager(
        IProductDal productDal,
        IInventoryDal inventoryDal,
        IUnitOfWork unitOfWork,
        ICacheService cacheService,
        ILogger<ProductManager> logger,
        IConfiguration configuration)
    {
        _productDal = productDal;
        _inventoryDal = inventoryDal;
        _unitOfWork = unitOfWork;
        _cacheService = cacheService;
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

    public async Task<IDataResult<ProductDto>> GetProductByIdAsync(int id)
    {
        var product = await _productDal.GetByIdWithDetailsAsync(id);
        
        if (product == null || !product.IsActive)
            return new ErrorDataResult<ProductDto>("Product not found");

        return new SuccessDataResult<ProductDto>(MapToDto(product));
    }

    public async Task<IDataResult<ProductDto>> CreateProductAsync(CreateProductRequest request)
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

        await _productDal.AddAsync(product);

        var inventory = new Inventory
        {
            Product = product,
            QuantityAvailable = request.InitialStock,
            QuantityReserved = 0
        };

        await _inventoryDal.AddAsync(inventory);

        await _unitOfWork.SaveChangesAsync();

        product.Inventory = inventory;
        return new SuccessDataResult<ProductDto>(MapToDto(product), "Product created successfully");
    }

    public async Task<IDataResult<ProductDto>> UpdateProductAsync(int id, UpdateProductRequest request)
    {
        var product = await _productDal.GetAsync(p => p.Id == id);
        
        if (product == null)
            return new ErrorDataResult<ProductDto>("Product not found");

        product.Name = request.Name;
        product.Description = request.Description;
        product.Price = request.Price;
        product.CategoryId = request.CategoryId;
        product.IsActive = request.IsActive;
        product.UpdatedAt = DateTime.UtcNow;

        _productDal.Update(product);
        await _unitOfWork.SaveChangesAsync();

        var updatedProduct = await _productDal.GetByIdWithDetailsAsync(id);
        return new SuccessDataResult<ProductDto>(MapToDto(updatedProduct!));
    }

    public async Task<IResult> DeleteProductAsync(int id)
    {
        var product = await _productDal.GetAsync(p => p.Id == id);
        
        if (product == null)
            return new ErrorResult("Product not found");

        product.IsActive = false;
        product.UpdatedAt = DateTime.UtcNow;

        _productDal.Update(product);
        await _unitOfWork.SaveChangesAsync();

        return new SuccessResult("Product deleted successfully");
    }

    public async Task<IResult> UpdateStockAsync(int productId, UpdateStockRequest request, int userId)
    {
        var inventory = await _inventoryDal.GetByProductIdAsync(productId);

        if (inventory == null)
            return new ErrorResult("Inventory not found");

        var newQuantity = inventory.QuantityAvailable + request.Delta;
        if (newQuantity < 0)
            return new ErrorResult("Insufficient stock");

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

        return new SuccessResult("Stock updated successfully");
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
