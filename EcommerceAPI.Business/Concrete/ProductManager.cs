using EcommerceAPI.Business.Abstract;
using EcommerceAPI.Entities.DTOs;
using EcommerceAPI.Core.Utilities.Results;
using EcommerceAPI.DataAccess.Abstract;
using EcommerceAPI.Core.Parameters;
using EcommerceAPI.Entities.Concrete;
using EcommerceAPI.Core.Interfaces;
using EcommerceAPI.Core.CrossCuttingConcerns.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using EcommerceAPI.Business.Extensions;
using EcommerceAPI.Core.Aspects.Autofac.Caching;
using EcommerceAPI.Core.Aspects.Autofac.Logging;
using EcommerceAPI.Core.Aspects.Autofac.Transaction;
using EcommerceAPI.Core.Aspects.Autofac.Validation;
using EcommerceAPI.Business.Validators;
using EcommerceAPI.Business.Constants;

namespace EcommerceAPI.Business.Concrete;

public class ProductManager : IProductService
{
    private readonly IProductDal _productDal;
    private readonly IInventoryDal _inventoryDal;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditService _auditService;
    private readonly int _productListCacheTTL;
    private readonly IProductSearchIndexService _productSearchIndexService;
    private readonly ILogger<ProductManager> _logger;


    public ProductManager(
        IProductDal productDal,
        IInventoryDal inventoryDal,
        IUnitOfWork unitOfWork,
        IConfiguration configuration,
        IAuditService auditService,
        IProductSearchIndexService productSearchIndexService,
        ILogger<ProductManager> logger)
    {
        _productDal = productDal;
        _inventoryDal = inventoryDal;
        _unitOfWork = unitOfWork;
        _auditService = auditService;
        _productListCacheTTL = configuration.GetValue<int>("Cache:ProductListTTLMinutes", 5);
        _productSearchIndexService = productSearchIndexService;
        _logger = logger;
    }

    [LogAspect]
    [CacheAspect(duration: 10)]
    public async Task<IDataResult<PaginatedResponse<ProductDto>>> GetProductsAsync(ProductListRequest request)
    {


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
            return new ErrorDataResult<ProductDto>(Messages.ProductNotFound);

        return new SuccessDataResult<ProductDto>(product.ToDto());
    }

    [LogAspect]
    [CacheRemoveAspect("products:")]
    [TransactionScopeAspect]
    [ValidationAspect(typeof(CreateProductRequestValidator))]
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


        await _auditService.LogActionAsync(
            sellerId?.ToString() ?? "Admin",
            "CreateProduct",
            "Product",
            new { ProductId = product.Id, ProductName = product.Name, Price = product.Price, SKU = product.SKU });

        product.Inventory = inventory;

        try
        {
            await _productSearchIndexService.IndexProductAsync(product.Id);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Product search index update failed on create for ProductId={ProductId}", product.Id);
        }


        return new SuccessDataResult<ProductDto>(product.ToDto(), Messages.ProductAdded);
    }

    [LogAspect]
    [CacheRemoveAspect("products:")]
    [TransactionScopeAspect]
    [ValidationAspect(typeof(UpdateProductRequestValidator))]
    public async Task<IDataResult<ProductDto>> UpdateProductAsync(int id, UpdateProductRequest request, int? sellerId = null)
    {
        var product = await _productDal.GetAsync(p => p.Id == id);
        
        if (product == null)
            return new ErrorDataResult<ProductDto>(Messages.ProductNotFound);


        if (sellerId.HasValue && product.SellerId != sellerId)
            return new ErrorDataResult<ProductDto>(Messages.AuthorizationDenied);

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




        await _auditService.LogActionAsync(
            sellerId?.ToString() ?? "Admin",
            "UpdateProduct",
            "Product",
            new { ProductId = product.Id, ProductName = product.Name, Price = product.Price, StockQuantity = request.StockQuantity });

        var updatedProduct = await _productDal.GetByIdWithDetailsAsync(id);

        try
        {
            if (!product.IsActive)
                await _productSearchIndexService.DeleteProductAsync(product.Id);
            else
                await _productSearchIndexService.IndexProductAsync(product.Id);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Product search index update failed on update for ProductId={ProductId}", product.Id);
        }

        return new SuccessDataResult<ProductDto>(updatedProduct!.ToDto(), Messages.ProductUpdated);
    }

    [LogAspect]
    [CacheRemoveAspect("products:")]
    [TransactionScopeAspect]
    public async Task<IResult> DeleteProductAsync(int id, int? sellerId = null)
    {
        var product = await _productDal.GetAsync(p => p.Id == id);
        
        if (product == null)
            return new ErrorResult(Messages.ProductNotFound);


        if (sellerId.HasValue && product.SellerId != sellerId)
            return new ErrorResult(Messages.AuthorizationDenied);

        product.IsActive = false;
        product.UpdatedAt = DateTime.UtcNow;

        _productDal.Update(product);
        await _unitOfWork.SaveChangesAsync();




        await _auditService.LogActionAsync(
            sellerId?.ToString() ?? "Admin",
            "DeleteProduct",
            "Product",
            new { ProductId = product.Id, ProductName = product.Name });

        try
        {
            await _productSearchIndexService.DeleteProductAsync(product.Id);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Product search index delete failed for ProductId={ProductId}", product.Id);
        }

        return new SuccessResult(Messages.ProductDeleted);
    }

    public async Task<IResult> UpdateStockAsync(int productId, UpdateStockRequest request, int userId)
    {
        var inventory = await _inventoryDal.GetByProductIdAsync(productId);

        if (inventory == null)
            return new ErrorResult(Messages.StockNotFound);

        var newQuantity = inventory.QuantityAvailable + request.Delta;
        if (newQuantity < 0)
            return new ErrorResult(Messages.StockInsufficient);

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

        try
        {
            await _productSearchIndexService.IndexProductAsync(productId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Product search index update failed on stock update for ProductId={ProductId}", productId);
        }

        return new SuccessResult(Messages.StockUpdated);
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
