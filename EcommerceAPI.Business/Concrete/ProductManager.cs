using EcommerceAPI.Business.Abstract;
using EcommerceAPI.Entities.DTOs;
using EcommerceAPI.Core.Utilities.Results;
using EcommerceAPI.DataAccess.Abstract;
using EcommerceAPI.Core.Parameters;
using EcommerceAPI.Entities.Concrete;
using EcommerceAPI.Core.Interfaces;
using EcommerceAPI.Core.CrossCuttingConcerns.Logging;
using EcommerceAPI.DataAccess.Concrete.EntityFramework.Contexts;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using EcommerceAPI.Business.Extensions;

namespace EcommerceAPI.Business.Concrete;

public class ProductManager : IProductService
{
    private readonly IProductDal _productDal;
    private readonly IInventoryDal _inventoryDal;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICacheService _cacheService;
    private readonly ILogger<ProductManager> _logger;
    private readonly IAuditService _auditService;
    private readonly int _productListCacheTTL;

    public ProductManager(
        IProductDal productDal,
        IInventoryDal inventoryDal,
        IUnitOfWork unitOfWork,
        ICacheService cacheService,
        ILogger<ProductManager> logger,
        IConfiguration configuration,
        IAuditService auditService)
    {
        _productDal = productDal;
        _inventoryDal = inventoryDal;
        _unitOfWork = unitOfWork;
        _cacheService = cacheService;
        _logger = logger;
        _auditService = auditService;
        _productListCacheTTL = configuration.GetValue<int>("Cache:ProductListTTLMinutes", 5);
    }

    public async Task<IDataResult<PaginatedResponse<ProductDto>>> GetProductsAsync(ProductListRequest request)
    {
        var cacheKey = BuildProductListCacheKey(request);


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
        var productDtos = items.Select(p => p.ToDto()).ToList();

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
        var (items, totalCount) = await _productDal.GetPagedForSellerAsync(
            request.Page, 
            request.PageSize, 
            sellerId, 
            request.CategoryId, 
            request.MinPrice, 
            request.MaxPrice, 
            request.Search, 
            request.SortBy, 
            request.SortDescending);

        var result = new PaginatedResponse<ProductDto>
        {
            Items = items.Select(p => p.ToDto()).ToList(),
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

        return new SuccessDataResult<ProductDto>(product.ToDto());
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
            SellerId = sellerId,
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


        await _cacheService.RemoveByPatternAsync("products:");

        _logger.LogInformation("Product created: {ProductId} by Seller: {SellerId}", product.Id, sellerId?.ToString() ?? "Admin");

        await _auditService.LogActionAsync(
            sellerId?.ToString() ?? "Admin",
            "CreateProduct",
            "Product",
            new { ProductId = product.Id, ProductName = product.Name, Price = product.Price, SKU = product.SKU });

        product.Inventory = inventory;
        return new SuccessDataResult<ProductDto>(product.ToDto(), "Ürün başarıyla oluşturuldu");
    }

    public async Task<IDataResult<ProductDto>> UpdateProductAsync(int id, UpdateProductRequest request, int? sellerId = null)
    {
        var product = await _productDal.GetAsync(p => p.Id == id);
        
        if (product == null)
            return new ErrorDataResult<ProductDto>("Ürün bulunamadı");


        if (sellerId.HasValue && product.SellerId != sellerId)
            return new ErrorDataResult<ProductDto>("Bu ürünü düzenleme yetkiniz yok");

        product.Name = request.Name;
        product.Description = request.Description;
        product.Price = request.Price;
        product.CategoryId = request.CategoryId;
        product.IsActive = request.IsActive;
        product.UpdatedAt = DateTime.UtcNow;

        _productDal.Update(product);


        if (request.StockQuantity.HasValue)
        {
            var inventory = await _inventoryDal.GetByProductIdAsync(id);
            if (inventory != null)
            {
                inventory.QuantityAvailable = request.StockQuantity.Value;
                _inventoryDal.Update(inventory);
            }
        }

        await _unitOfWork.SaveChangesAsync();


        await _cacheService.RemoveByPatternAsync("products:");

        await _auditService.LogActionAsync(
            sellerId?.ToString() ?? "Admin",
            "UpdateProduct",
            "Product",
            new { ProductId = product.Id, ProductName = product.Name, Price = product.Price, StockQuantity = request.StockQuantity });

        var updatedProduct = await _productDal.GetByIdWithDetailsAsync(id);
        return new SuccessDataResult<ProductDto>(updatedProduct!.ToDto(), "Ürün başarıyla güncellendi");
    }

    public async Task<IResult> DeleteProductAsync(int id, int? sellerId = null)
    {
        var product = await _productDal.GetAsync(p => p.Id == id);
        
        if (product == null)
            return new ErrorResult("Ürün bulunamadı");


        if (sellerId.HasValue && product.SellerId != sellerId)
            return new ErrorResult("Bu ürünü silme yetkiniz yok");

        product.IsActive = false;
        product.UpdatedAt = DateTime.UtcNow;

        _productDal.Update(product);
        await _unitOfWork.SaveChangesAsync();


        await _cacheService.RemoveByPatternAsync("products:");

        await _auditService.LogActionAsync(
            sellerId?.ToString() ?? "Admin",
            "DeleteProduct",
            "Product",
            new { ProductId = product.Id, ProductName = product.Name });

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

        await _auditService.LogActionAsync(
            userId.ToString(),
            "UpdateStock",
            "Inventory",
            new { ProductId = productId, Delta = request.Delta, NewQuantity = newQuantity, Reason = request.Reason });

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


}
