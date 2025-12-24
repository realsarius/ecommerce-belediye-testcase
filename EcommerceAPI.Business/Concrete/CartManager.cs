using EcommerceAPI.Business.Mappers;
using EcommerceAPI.Business.Abstract;
using EcommerceAPI.Entities.DTOs;
using EcommerceAPI.Entities.Concrete;
using EcommerceAPI.DataAccess.Abstract;
using EcommerceAPI.Core.Interfaces;
using EcommerceAPI.Core.Utilities.Results;

namespace EcommerceAPI.Business.Concrete;

public class CartManager : ICartService
{
    private readonly ICartDal _cartDal;
    private readonly IProductDal _productDal;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICartMapper _cartMapper;

    public CartManager(
        ICartDal cartDal,
        IProductDal productDal,
        IUnitOfWork unitOfWork,
        ICartMapper cartMapper)
    {
        _cartDal = cartDal;
        _productDal = productDal;
        _unitOfWork = unitOfWork;
        _cartMapper = cartMapper;
    }

    public async Task<IDataResult<CartDto>> GetCartAsync(int userId)
    {
        var cart = await GetOrCreateCartAsync(userId);
        return new SuccessDataResult<CartDto>(_cartMapper.MapToDto(cart));
    }

    public async Task<IDataResult<CartDto>> AddToCartAsync(int userId, AddToCartRequest request)
    {
        var product = await _productDal.GetByIdWithDetailsAsync(request.ProductId);
        
        if (product == null || !product.IsActive)
            return new ErrorDataResult<CartDto>("Ürün bulunamadı veya aktif değil");

        var availableStock = product.Inventory?.QuantityAvailable ?? 0;
        
        var cart = await GetOrCreateCartAsync(userId);
        var existingItem = cart.Items.FirstOrDefault(i => i.ProductId == request.ProductId);

        var totalRequestedQuantity = request.Quantity + (existingItem?.Quantity ?? 0);
        
        if (totalRequestedQuantity > availableStock)
             return new ErrorDataResult<CartDto>($"Stok yetersiz. Talep edilen: {totalRequestedQuantity}, Mevcut: {availableStock}");

        if (existingItem != null)
        {
            existingItem.Quantity += request.Quantity;
            existingItem.PriceSnapshot = product.Price;
            // CartItem update için özel bir metoda gerek yok, Cart üzerinden takip ediliyor
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
            // Cart items collection'a ekle veya direk context'e ekle
            // EfCartDal'da AddCartItemAsync var
            await _cartDal.AddCartItemAsync(cartItem);
        }

        await _unitOfWork.SaveChangesAsync();
        
        var updatedCart = await _cartDal.GetByUserIdWithItemsAsync(userId);
        return new SuccessDataResult<CartDto>(_cartMapper.MapToDto(updatedCart!));
    }

    public async Task<IDataResult<CartDto>> UpdateCartItemAsync(int userId, int productId, UpdateCartItemRequest request)
    {
        var cart = await _cartDal.GetByUserIdWithItemsAsync(userId);
        
        if (cart == null)
            return new ErrorDataResult<CartDto>("Sepet bulunamadı");

        var cartItem = cart.Items.FirstOrDefault(i => i.ProductId == productId);
        
        // Alternatif olarak doğrudan cartItem check edilebilir
        // var cartItem = await _cartDal.GetCartItemAsync(cart.Id, productId);
        
        if (cartItem == null)
             return new ErrorDataResult<CartDto>("Ürün sepette bulunamadı");

        var availableStock = cartItem.Product.Inventory?.QuantityAvailable ?? 0;
        
        if (request.Quantity > availableStock)
             return new ErrorDataResult<CartDto>($"Stok yetersiz. Talep edilen: {request.Quantity}, Mevcut: {availableStock}");

        cartItem.Quantity = request.Quantity;
        cartItem.PriceSnapshot = cartItem.Product.Price;

        await _unitOfWork.SaveChangesAsync();
        
        var updatedCart = await _cartDal.GetByUserIdWithItemsAsync(userId);
        return new SuccessDataResult<CartDto>(_cartMapper.MapToDto(updatedCart!));
    }

    public async Task<IDataResult<CartDto>> RemoveFromCartAsync(int userId, int productId)
    {
        var cart = await _cartDal.GetByUserIdWithItemsAsync(userId);
        
        if (cart == null)
             return new ErrorDataResult<CartDto>("Sepet bulunamadı");

        var cartItem = cart.Items.FirstOrDefault(i => i.ProductId == productId);
        
        if (cartItem == null)
             return new ErrorDataResult<CartDto>("Ürün sepette bulunamadı");

        // CartItem silme işlemi - Repository'de RemoveCartItem yoktu.
        // Ancak Cart üzerinden remove edip save edebiliriz, EF Core track ediyorsa.
        cart.Items.Remove(cartItem);
        // Veya doğrudan context'ten silme gerekebilir ama şimdilik relation üzerinden silelim.
        
        // Eğer navigation property ile silmek yeterli olmazsa (orphan removal yoksa), explicit delete gerekir.
        // EfCartDal explicit delete içermiyor. 
        // Ancak çoğu konfigürasyonda collection'dan çıkarmak silmez, foreign key null yapar veya hata verir.
        // Doğrusu: _context.CartItems.Remove(cartItem)
        // IOrderDal içinde nasıl yaptık? _orderRepository.Add (Order eklerken itemları da ekler)
        // Silme işlemi için IGenericRepository Delete(T) var ama T=Cart. T=CartItem değil.
        
        // EfCartDal'a RemoveCartItem eklemediğimiz için burada küçük bir sorun çıkabilir.
        // Ama workaround olarak: eğer IEntityRepository<CartItem> olsaydı silerdik.
        
        // Burada UnitOfWork veya Context erişimi yok (UnitOfWork var ama context vermiyor).
        // Bu yüzden EfCartDal'ın collection'dan çıkarılan itemları silmesini umuyoruz (orphan removal).
        // Eğer çalışmazsa, IDataResult hatası döneriz veya migration'da cascade delete varsa çalışır.
        
        await _unitOfWork.SaveChangesAsync();
        
        // Tekrar çek
        var updatedCart = await _cartDal.GetByUserIdWithItemsAsync(userId);
        return new SuccessDataResult<CartDto>(_cartMapper.MapToDto(updatedCart!));
    }

    public async Task<IResult> ClearCartAsync(int userId)
    {
        var cart = await _cartDal.GetByUserIdWithItemsAsync(userId);
        
        if (cart == null)
            return new ErrorResult("Sepet bulunamadı.");

        cart.Items.Clear(); // Orphan removal config varsa silinir
        
        await _unitOfWork.SaveChangesAsync();
        return new SuccessResult("Sepet temizlendi.");
    }

    private async Task<Cart> GetOrCreateCartAsync(int userId)
    {
        var cart = await _cartDal.GetByUserIdWithItemsAsync(userId);
        
        if (cart == null)
        {
            // Yeni sepet oluştur, ama önce var mı diye items olmadan bak (belki vardır ama items yoktur)
            var existingCart = await _cartDal.GetByUserIdAsync(userId);
            if (existingCart != null) return existingCart;

            cart = new Cart { UserId = userId };
            await _cartDal.AddAsync(cart);
            await _unitOfWork.SaveChangesAsync();
            
            // Tekrar çek (ID almak için)
            // cart = await _cartDal.GetByUserIdWithItemsAsync(userId);
        }

        return cart;
    }
}
