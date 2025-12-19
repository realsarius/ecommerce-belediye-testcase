using EcommerceAPI.Business.Mappers;
using EcommerceAPI.Business.Services.Abstract;
using EcommerceAPI.Core.DTOs;
using EcommerceAPI.Core.Entities;
using EcommerceAPI.Core.Exceptions;
using EcommerceAPI.Core.Interfaces;

namespace EcommerceAPI.Business.Services.Concrete;

public class CartService : ICartService
{
    private readonly ICartRepository _cartRepository;
    private readonly IProductRepository _productRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICartMapper _cartMapper;

    public CartService(
        ICartRepository cartRepository,
        IProductRepository productRepository,
        IUnitOfWork unitOfWork,
        ICartMapper cartMapper)
    {
        _cartRepository = cartRepository;
        _productRepository = productRepository;
        _unitOfWork = unitOfWork;
        _cartMapper = cartMapper;
    }

    public async Task<CartDto> GetCartAsync(int userId)
    {
        var cart = await GetOrCreateCartAsync(userId);
        return _cartMapper.MapToDto(cart);
    }

    public async Task<CartDto> AddToCartAsync(int userId, AddToCartRequest request)
    {
        var product = await _productRepository.GetByIdWithDetailsAsync(request.ProductId);
        
        if (product == null || !product.IsActive)
            throw new NotFoundException("Ürün", request.ProductId, "Ürün bulunamadı veya aktif değil");

        var availableStock = product.Inventory?.QuantityAvailable ?? 0;
        
        var cart = await GetOrCreateCartAsync(userId);
        var existingItem = cart.Items.FirstOrDefault(i => i.ProductId == request.ProductId);

        var totalRequestedQuantity = request.Quantity + (existingItem?.Quantity ?? 0);
        
        if (totalRequestedQuantity > availableStock)
            throw new InsufficientStockException(request.ProductId, totalRequestedQuantity, availableStock);

        if (existingItem != null)
        {
            existingItem.Quantity += request.Quantity;
            existingItem.PriceSnapshot = product.Price;
        }
        else
        {
            var cartItem = new CartItem
            {
                CartId = cart.Id,
                ProductId = request.ProductId,
                Quantity = request.Quantity,
                PriceSnapshot = product.Price
            };
            cart.Items.Add(cartItem);
        }

        await _unitOfWork.SaveChangesAsync();
        
        var updatedCart = await _cartRepository.GetCartWithItemsAsync(cart.Id);
        return _cartMapper.MapToDto(updatedCart!);
    }

    public async Task<CartDto> UpdateCartItemAsync(int userId, int productId, UpdateCartItemRequest request)
    {
        var cart = await _cartRepository.GetActiveCartByUserIdAsync(userId);
        
        if (cart == null)
            throw new NotFoundException("Sepet", userId);

        var cartItem = await _cartRepository.GetCartItemAsync(cart.Id, productId);
        
        if (cartItem == null)
            throw new NotFoundException("Sepet öğesi", productId, "Ürün sepette bulunamadı");

        var availableStock = cartItem.Product.Inventory?.QuantityAvailable ?? 0;
        
        if (request.Quantity > availableStock)
            throw new InsufficientStockException(productId, request.Quantity, availableStock);

        cartItem.Quantity = request.Quantity;
        cartItem.PriceSnapshot = cartItem.Product.Price;

        await _unitOfWork.SaveChangesAsync();
        
        var updatedCart = await _cartRepository.GetCartWithItemsAsync(cart.Id);
        return _cartMapper.MapToDto(updatedCart!);
    }

    public async Task<CartDto> RemoveFromCartAsync(int userId, int productId)
    {
        var cart = await _cartRepository.GetActiveCartByUserIdAsync(userId);
        
        if (cart == null)
            throw new NotFoundException("Sepet", userId);

        var cartItem = await _cartRepository.GetCartItemAsync(cart.Id, productId);
        
        if (cartItem == null)
            throw new NotFoundException("Sepet öğesi", productId, "Ürün sepette bulunamadı");

        _cartRepository.RemoveCartItem(cartItem);
        await _unitOfWork.SaveChangesAsync();
        
        var updatedCart = await _cartRepository.GetCartWithItemsAsync(cart.Id);
        return _cartMapper.MapToDto(updatedCart!);
    }

    public async Task<bool> ClearCartAsync(int userId)
    {
        var cart = await _cartRepository.GetActiveCartByUserIdAsync(userId);
        
        if (cart == null)
            return false;

        foreach (var item in cart.Items.ToList())
        {
            _cartRepository.RemoveCartItem(item);
        }

        await _unitOfWork.SaveChangesAsync();
        return true;
    }

    private async Task<Cart> GetOrCreateCartAsync(int userId)
    {
        var cart = await _cartRepository.GetActiveCartByUserIdAsync(userId);
        
        if (cart == null)
        {
            cart = new Cart { UserId = userId };
            await _cartRepository.AddAsync(cart);
            await _unitOfWork.SaveChangesAsync();
            
            cart = await _cartRepository.GetActiveCartByUserIdAsync(userId);
        }

        return cart!;
    }
}
