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
using EcommerceAPI.Entities.IntegrationEvents;
using MassTransit;

namespace EcommerceAPI.Business.Concrete;

public class ProductManager : IProductService
{
    private const int WishlistLowStockThreshold = 5;
    private readonly IProductDal _productDal;
    private readonly IInventoryDal _inventoryDal;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditService _auditService;
    private readonly int _productListCacheTTL;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<ProductManager> _logger;


    public ProductManager(
        IProductDal productDal,
        IInventoryDal inventoryDal,
        IUnitOfWork unitOfWork,
        IConfiguration configuration,
        IAuditService auditService,
        ILogger<ProductManager> logger,
        IPublishEndpoint publishEndpoint)
    {
        _productDal = productDal;
        _inventoryDal = inventoryDal;
        _unitOfWork = unitOfWork;
        _auditService = auditService;
        _productListCacheTTL = configuration.GetValue<int>("Cache:ProductListTTLMinutes", 5);
        _logger = logger;
        _publishEndpoint = publishEndpoint;
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

        await QueueProductIndexSyncEventAsync(product.Id, ProductIndexOperations.Upsert, "CreateProduct");
        await _unitOfWork.SaveChangesAsync();


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

        await QueueProductIndexSyncEventAsync(
            product.Id,
            product.IsActive ? ProductIndexOperations.Upsert : ProductIndexOperations.Delete,
            "UpdateProduct");
        await _unitOfWork.SaveChangesAsync();

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

        await QueueProductIndexSyncEventAsync(product.Id, ProductIndexOperations.Delete, "DeleteProduct");
        await _unitOfWork.SaveChangesAsync();

        return new SuccessResult(Messages.ProductDeleted);
    }

    [TransactionScopeAspect]
    public async Task<IResult> UpdateStockAsync(int productId, UpdateStockRequest request, int userId)
    {
        var inventory = await _inventoryDal.GetByProductIdAsync(productId);

        if (inventory == null)
            return new ErrorResult(Messages.StockNotFound);

        var newQuantity = inventory.QuantityAvailable + request.Delta;
        if (newQuantity < 0)
            return new ErrorResult(Messages.StockInsufficient);

        var oldQuantity = inventory.QuantityAvailable;
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

        if (ShouldPublishLowStockAlert(oldQuantity, newQuantity))
        {
            await PublishWishlistLowStockEventAsync(productId, newQuantity, request.Reason);
        }

        await QueueProductIndexSyncEventAsync(productId, ProductIndexOperations.Upsert, "UpdateStock");
        await _unitOfWork.SaveChangesAsync();

        return new SuccessResult(Messages.StockUpdated);
    }

    public async Task<bool> IsProductOwnedBySellerAsync(int productId, int sellerId)
    {
        var product = await _productDal.GetAsync(p => p.Id == productId);
        return product?.SellerId == sellerId;
    }

    private async Task QueueProductIndexSyncEventAsync(int productId, string operation, string reason)
    {
        var eventMessage = new ProductIndexSyncEvent
        {
            ProductId = productId,
            Operation = operation,
            Reason = reason
        };

        await _publishEndpoint.Publish(eventMessage);

        _logger.LogInformation(
            "ProductIndexSyncEvent queued to MassTransit bus outbox. ProductId={ProductId}, Operation={Operation}, Reason={Reason}",
            productId,
            operation,
            reason);
    }

    private async Task PublishWishlistLowStockEventAsync(int productId, int stockQuantity, string reason)
    {
        var eventMessage = new WishlistProductLowStockEvent
        {
            ProductId = productId,
            StockQuantity = stockQuantity,
            Threshold = WishlistLowStockThreshold,
            Reason = reason
        };

        await _publishEndpoint.Publish(eventMessage);

        _logger.LogInformation(
            "WishlistProductLowStockEvent queued to MassTransit bus outbox. ProductId={ProductId}, StockQuantity={StockQuantity}, Threshold={Threshold}",
            productId,
            stockQuantity,
            WishlistLowStockThreshold);
    }

    private static bool ShouldPublishLowStockAlert(int oldStock, int newStock)
    {
        return oldStock > WishlistLowStockThreshold &&
               newStock > 0 &&
               newStock <= WishlistLowStockThreshold;
    }

    private static string BuildProductListCacheKey(ProductListRequest request)
    {
        return $"products:{request.Page}:{request.PageSize}:{request.CategoryId}:{request.MinPrice}:{request.MaxPrice}:{request.Search}:{request.InStock}:{request.SortBy}:{request.SortDescending}";
    }


}
