using EcommerceAPI.Business.Abstract;
using EcommerceAPI.Entities.DTOs;
using EcommerceAPI.DataAccess.Abstract;
using EcommerceAPI.Core.Interfaces;
using EcommerceAPI.Core.Utilities.Results;
using EcommerceAPI.Core.Aspects.Autofac.Logging;
using EcommerceAPI.Core.Aspects.Autofac.Validation;
using EcommerceAPI.Business.Validators;
using EcommerceAPI.Business.Constants;
using EcommerceAPI.Business.Extensions;
using EcommerceAPI.Entities.Concrete;

namespace EcommerceAPI.Business.Concrete;

/// <summary>
/// Cart management service.
/// </summary>
public class CartManager : ICartService
{
    private readonly ICartCacheService _cartCache;
    private readonly IProductDal _productDal;
    private readonly IOrderDal _orderDal;
    
    public CartManager(
        ICartCacheService cartCache,
        IProductDal productDal,
        IOrderDal orderDal)
    {
        _cartCache = cartCache;
        _productDal = productDal;
        _orderDal = orderDal;
    }

    [LogAspect]
    public async Task<IDataResult<CartDto>> GetCartAsync(int userId)
    {
        var cartItems = await _cartCache.GetCartItemsAsync(userId);
        
        var cartDto = new CartDto
        {
            Id = 0,
            Items = new List<CartItemDto>(),
            TotalAmount = 0,
            TotalItems = 0
        };

        if (cartItems.Count == 0)
        {
            return new SuccessDataResult<CartDto>(cartDto);
        }

        foreach (var (productId, quantity) in cartItems)
        {
            var product = await _productDal.GetByIdWithDetailsAsync(productId);
            if (product != null && product.IsActive)
            {
                var price = product.GetEffectivePrice();
                var itemTotal = price * quantity;
                
                cartDto.Items.Add(new CartItemDto
                {
                    Id = 0,
                    ProductId = productId,
                    ProductName = product.Name,
                    ProductSKU = product.SKU,
                    Quantity = quantity,
                    UnitPrice = price,
                    TotalPrice = itemTotal,
                    AvailableStock = product.Inventory?.QuantityAvailable ?? 0
                });

                cartDto.TotalAmount += itemTotal;
                cartDto.TotalItems += quantity;
            }
            else
            {
                await _cartCache.RemoveItemAsync(userId, productId);
            }
        }

        return new SuccessDataResult<CartDto>(cartDto);
    }

    [LogAspect]
    [ValidationAspect(typeof(AddToCartRequestValidator))]
    public async Task<IDataResult<CartDto>> AddToCartAsync(int userId, AddToCartRequest request)
    {
        var product = await _productDal.GetByIdWithDetailsAsync(request.ProductId);
        
        if (product == null || !product.IsActive)
            return new ErrorDataResult<CartDto>(Messages.ProductNotFound);

        var availableStock = product.Inventory?.QuantityAvailable ?? 0;
        
        var currentQty = await _cartCache.GetItemQuantityAsync(userId, request.ProductId);
        var totalRequestedQuantity = request.Quantity + currentQty;
        
        if (totalRequestedQuantity > availableStock)
            return new ErrorDataResult<CartDto>($"{Messages.StockInsufficient}. Talep edilen: {totalRequestedQuantity}, Mevcut: {availableStock}");

        await _cartCache.IncrementItemQuantityAsync(userId, request.ProductId, request.Quantity);
        
        return await GetCartAsync(userId);
    }

    [LogAspect]
    [ValidationAspect(typeof(ReorderCartRequestValidator))]
    public async Task<IDataResult<ReorderCartResultDto>> ReorderAsync(int userId, ReorderCartRequest request)
    {
        var order = await _orderDal.GetByIdWithDetailsAsync(request.OrderId);
        if (order == null || order.UserId != userId)
        {
            return new ErrorDataResult<ReorderCartResultDto>(Messages.OrderNotFound);
        }

        var distinctOrderItems = order.OrderItems
            .GroupBy(item => item.ProductId)
            .Select(group => new ReorderableOrderItem
            {
                ProductId = group.Key,
                Quantity = group.Sum(item => item.Quantity),
                ProductName = group.Select(item => item.Product?.Name).FirstOrDefault(name => !string.IsNullOrWhiteSpace(name))
            })
            .ToList();

        if (distinctOrderItems.Count == 0)
        {
            return new SuccessDataResult<ReorderCartResultDto>(
                new ReorderCartResultDto(),
                "Siparişte tekrar sepete eklenecek ürün bulunamadı.");
        }

        var products = await _productDal.GetByIdsWithInventoryAsync(distinctOrderItems.Select(item => item.ProductId).Distinct().ToList());
        var productsById = products.ToDictionary(product => product.Id);
        var currentCartItems = await _cartCache.GetCartItemsAsync(userId);

        var result = new ReorderCartResultDto
        {
            RequestedCount = distinctOrderItems.Count
        };

        foreach (var orderItem in distinctOrderItems)
        {
            if (!productsById.TryGetValue(orderItem.ProductId, out var product) || !product.IsActive)
            {
                result.SkippedProducts.Add(CreateSkippedProduct(orderItem.ProductId, orderItem.ProductName, "Ürün artık satışta değil."));
                continue;
            }

            var availableStock = product.Inventory?.QuantityAvailable ?? 0;
            if (availableStock <= 0)
            {
                result.SkippedProducts.Add(CreateSkippedProduct(product.Id, product.Name, "Ürün stokta yok."));
                continue;
            }

            currentCartItems.TryGetValue(product.Id, out var currentQuantity);
            var remainingCapacity = Math.Max(0, availableStock - currentQuantity);
            if (remainingCapacity <= 0)
            {
                result.SkippedProducts.Add(CreateSkippedProduct(product.Id, product.Name, "Sepetteki miktar stok sınırına ulaştı."));
                continue;
            }

            var quantityToAdd = Math.Min(orderItem.Quantity, remainingCapacity);
            await _cartCache.IncrementItemQuantityAsync(userId, product.Id, quantityToAdd);
            currentCartItems[product.Id] = currentQuantity + quantityToAdd;
            result.AddedCount++;

            if (quantityToAdd < orderItem.Quantity)
            {
                result.SkippedProducts.Add(CreateSkippedProduct(
                    product.Id,
                    product.Name,
                    $"Siparişteki {orderItem.Quantity} adedin yalnızca {quantityToAdd} adedi sepete eklendi; kalan miktar stok nedeniyle atlandı."));
            }
        }

        result.SkippedCount = result.SkippedProducts.Count;

        return new SuccessDataResult<ReorderCartResultDto>(
            result,
            BuildReorderSummaryMessage(result));
    }

    [LogAspect]
    [ValidationAspect(typeof(UpdateCartItemRequestValidator))]
    public async Task<IDataResult<CartDto>> UpdateCartItemAsync(int userId, int productId, UpdateCartItemRequest request)
    {
        var product = await _productDal.GetByIdWithDetailsAsync(productId);
        if (product == null || !product.IsActive)
            return new ErrorDataResult<CartDto>(Messages.ProductNotFound);

        var availableStock = product.Inventory?.QuantityAvailable ?? 0;
        
        if (request.Quantity > availableStock)
            return new ErrorDataResult<CartDto>($"{Messages.StockInsufficient}. Talep edilen: {request.Quantity}, Mevcut: {availableStock}");

        if (!await _cartCache.ItemExistsAsync(userId, productId))
            return new ErrorDataResult<CartDto>(Messages.ProductNotFound);

        await _cartCache.SetItemQuantityAsync(userId, productId, request.Quantity);

        return await GetCartAsync(userId);
    }

    [LogAspect]
    public async Task<IDataResult<CartDto>> RemoveFromCartAsync(int userId, int productId)
    {
        if (!await _cartCache.ItemExistsAsync(userId, productId))
            return new ErrorDataResult<CartDto>(Messages.ProductNotFound);

        await _cartCache.RemoveItemAsync(userId, productId);
        
        return await GetCartAsync(userId);
    }

    [LogAspect]
    public async Task<IResult> ClearCartAsync(int userId)
    {
        await _cartCache.ClearCartAsync(userId);
        
        return new SuccessResult("Sepet temizlendi.");
    }

    private static ReorderCartSkippedProductDto CreateSkippedProduct(int productId, string? productName, string reason)
    {
        return new ReorderCartSkippedProductDto
        {
            ProductId = productId,
            Name = productName ?? $"Ürün #{productId}",
            Reason = reason
        };
    }

    private static string BuildReorderSummaryMessage(ReorderCartResultDto result)
    {
        if (result.RequestedCount == 0)
        {
            return "Siparişte tekrar sepete eklenecek ürün bulunamadı.";
        }

        if (result.SkippedCount == 0)
        {
            return $"{result.AddedCount} ürün sepete eklendi.";
        }

        if (result.AddedCount == 0)
        {
            return $"{result.RequestedCount} ürünün hiçbiri sepete eklenemedi.";
        }

        return $"{result.RequestedCount} üründen {result.AddedCount} ürün sepete eklendi, {result.SkippedCount} ürün atlandı.";
    }

    private sealed class ReorderableOrderItem
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; }
        public string? ProductName { get; set; }
    }
}
