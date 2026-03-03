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

    public async Task<IDataResult<ProductDto>> GetProductForSellerAsync(int id, int sellerId)
    {
        var product = await _productDal.GetByIdWithDetailsAsync(id);

        if (product == null)
            return new ErrorDataResult<ProductDto>(Messages.ProductNotFound);

        if (product.SellerId != sellerId)
            return new ErrorDataResult<ProductDto>(Messages.AuthorizationDenied);

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
            Currency = string.IsNullOrWhiteSpace(request.Currency) ? "TRY" : request.Currency.Trim().ToUpperInvariant(),
            SKU = request.SKU,
            CategoryId = request.CategoryId,
            SellerId = sellerId,
            IsActive = request.IsActive,
            Images = NormalizeImages(request.Images),
            Variants = NormalizeVariants(request.Variants)
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
        var product = await _productDal.GetByIdForUpdateAsync(id);
        
        if (product == null)
            return new ErrorDataResult<ProductDto>(Messages.ProductNotFound);


        if (sellerId.HasValue && product.SellerId != sellerId)
            return new ErrorDataResult<ProductDto>(Messages.AuthorizationDenied);

        product.Name = request.Name;
        product.Description = request.Description;
        product.Price = request.Price;
        product.Currency = string.IsNullOrWhiteSpace(request.Currency) ? "TRY" : request.Currency.Trim().ToUpperInvariant();
        product.SKU = request.SKU;
        product.CategoryId = request.CategoryId;
        product.IsActive = request.IsActive;
        product.UpdatedAt = DateTime.UtcNow;
        ReplaceImages(product, request.Images);
        ReplaceVariants(product, request.Variants);

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

    private static List<ProductImage> NormalizeImages(IEnumerable<ProductImageInputDto>? requestImages)
    {
        var normalized = (requestImages ?? [])
            .Where(image => !string.IsNullOrWhiteSpace(image.ImageUrl))
            .Select((image, index) => new ProductImage
            {
                ImageUrl = image.ImageUrl.Trim(),
                SortOrder = image.SortOrder > 0 ? image.SortOrder : index,
                IsPrimary = image.IsPrimary
            })
            .ToList();

        if (normalized.Count == 0)
        {
            return normalized;
        }

        if (normalized.All(image => !image.IsPrimary))
        {
            normalized[0].IsPrimary = true;
        }
        else
        {
            var primaryIndex = normalized.FindIndex(image => image.IsPrimary);
            for (var index = 0; index < normalized.Count; index++)
            {
                normalized[index].IsPrimary = index == primaryIndex;
            }
        }

        for (var index = 0; index < normalized.Count; index++)
        {
            normalized[index].SortOrder = index;
        }

        return normalized;
    }

    private static List<ProductVariant> NormalizeVariants(IEnumerable<ProductVariantInputDto>? requestVariants)
    {
        return (requestVariants ?? [])
            .Where(variant => !string.IsNullOrWhiteSpace(variant.Name) && !string.IsNullOrWhiteSpace(variant.Value))
            .Select((variant, index) => new ProductVariant
            {
                Name = variant.Name.Trim(),
                Value = variant.Value.Trim(),
                SortOrder = variant.SortOrder > 0 ? variant.SortOrder : index
            })
            .OrderBy(variant => variant.SortOrder)
            .ToList();
    }

    private static void ReplaceImages(Product product, IEnumerable<ProductImageInputDto>? requestImages)
    {
        product.Images.Clear();

        foreach (var image in NormalizeImages(requestImages))
        {
            product.Images.Add(image);
        }
    }

    private static void ReplaceVariants(Product product, IEnumerable<ProductVariantInputDto>? requestVariants)
    {
        product.Variants.Clear();

        foreach (var variant in NormalizeVariants(requestVariants))
        {
            product.Variants.Add(variant);
        }
    }

    [LogAspect]
    [CacheRemoveAspect("products:")]
    [TransactionScopeAspect]
    public async Task<IResult> BulkUpdateProductsAsync(BulkUpdateProductsRequest request)
    {
        var productIds = request.Ids
            .Where(id => id > 0)
            .Distinct()
            .ToList();

        if (productIds.Count == 0)
        {
            return new ErrorResult("Toplu işlem için en az bir ürün seçilmelidir.");
        }

        var normalizedAction = request.Action.Trim().ToLowerInvariant();
        if (normalizedAction is not ("activate" or "deactivate" or "delete"))
        {
            return new ErrorResult("Geçersiz toplu ürün işlemi.");
        }

        var products = await _productDal.GetListAsync(product => productIds.Contains(product.Id));
        if (products.Count == 0)
        {
            return new ErrorResult(Messages.ProductNotFound);
        }

        var now = DateTime.UtcNow;
        var shouldActivate = normalizedAction == "activate";

        foreach (var product in products)
        {
            product.IsActive = shouldActivate;
            product.UpdatedAt = now;
            _productDal.Update(product);
        }

        await _unitOfWork.SaveChangesAsync();

        await _auditService.LogActionAsync(
            "Admin",
            "BulkUpdateProducts",
            "Product",
            new
            {
                Action = normalizedAction,
                ProductIds = products.Select(product => product.Id).ToList(),
                Count = products.Count
            });

        var syncOperation = shouldActivate
            ? ProductIndexOperations.Upsert
            : ProductIndexOperations.Delete;

        foreach (var product in products)
        {
            await QueueProductIndexSyncEventAsync(product.Id, syncOperation, "BulkUpdateProducts");
        }

        await _unitOfWork.SaveChangesAsync();

        return new SuccessResult($"{products.Count} ürün için toplu işlem tamamlandı.");
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
