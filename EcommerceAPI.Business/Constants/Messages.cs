namespace EcommerceAPI.Business.Constants;

public static class Messages
{
    // General
    public static string MaintenanceTime = "Sistem bakımda";
    public static string AuthorizationDenied = "Yetkiniz yok";
    public static string InternalServerError = "Sunucu hatası";

    // Product
    public static string ProductAdded = "Ürün eklendi";
    public static string ProductNameInvalid = "Ürün ismi geçersiz";
    public static string ProductNotFound = "Ürün bulunamadı";
    public static string ProductUpdated = "Ürün güncellendi";
    public static string ProductDeleted = "Ürün silindi";
    public static string ProductFetched = "Ürün getirildi";
    public static string ProductsListed = "Ürünler listelendi";
    public static string ProductExists = "Bu ürün zaten mevcut";
    public static string ProductNameAlreadyExists = "Bu isimde bir ürün zaten mevcut";
    public static string ProductCountExceeded = "Bir kategoride en fazla 10 ürün olabilir";

    // Category
    public static string CategoryAdded = "Kategori eklendi";
    public static string CategoryNameInvalid = "Kategori ismi geçersiz";
    public static string CategoryUpdated = "Kategori güncellendi";
    public static string CategoryDeleted = "Kategori silindi";
    public static string CategoryNotFound = "Kategori bulunamadı";
    public static string CategoriesListed = "Kategoriler listelendi";
    public static string TotalCategoryNumberIsMoreThanFifteen = "Toplam kategori sayısı 15'ten fazla olamaz";
    public static string CategoryLimitExceeded = "Kategori limiti aşıldığı için yeni ürün eklenemiyor";
    public static string CategoryExists = "Bu kategori zaten mevcut";

    // Auth
    public static string UserRegistered = "Kullanıcı başarıyla kayıt oldu";
    public static string UserNotFound = "Kullanıcı bulunamadı";
    public static string PasswordError = "Şifre hatalı";
    public static string SuccessfulLogin = "Giriş başarılı";
    public static string UserAlreadyExists = "Bu kullanıcı zaten mevcut";
    public static string AccessTokenCreated = "Access token başarıyla oluşturuldu";
    public static string TokenRevoked = "Token başarıyla iptal edildi";
    public static string RefreshTokenNotFound = "Refresh token bulunamadı";
    public static string TokenInvalid = "Geçersiz token";
    public static string TokenExpired = "Token süresi dolmuş";
    public static string TokenRefreshed = "Token yenilendi";

    // Order
    public static string OrderNotFound = "Sipariş bulunamadı";
    public static string OrderCreated = "Sipariş başarıyla oluşturuldu";
    public static string OrderCancelled = "Sipariş iptal edildi";
    public static string OrderStatusUpdated = "Sipariş durumu güncellendi";
    public static string OrderItemsUpdated = "Sipariş güncellendi";
    public static string InvalidOrderStatus = "Geçersiz sipariş durumu";
    public static string OrderNotBelongToUser = "Sipariş size ait ürünler içermiyor";
    public static string OnlyPendingOrderCanBeCancelled = "Sadece ödeme bekleyen siparişler iptal edilebilir";
    public static string OnlyPendingOrderCanBeUpdated = "Sadece ödeme bekleyen siparişler düzenlenebilir";
    public static string OrderMustHaveItems = "Sipariş en az bir ürün içermelidir";
    public static string SomeProductsNotFound = "Bazı ürünler bulunamadı";

    // Cart
    public static string CartNotFound = "Sepet bulunamadı";
    public static string CartEmpty = "Sepetiniz boş. Sipariş oluşturmak için sepete ürün ekleyin.";

    // Coupon
    public static string CouponNotFound = "Kupon bulunamadı";
    public static string CouponExpired = "Kupon süresi dolmuş";
    public static string CouponUsageLimitExceeded = "Kupon kullanım limiti dolmuş";
    public static string CouponMinAmountNotMet = "Kupon için minimum sepet tutarı sağlanmadı";
    public static string CouponInvalid = "Geçersiz kupon kodu";
    public static string CouponCreated = "Kupon başarıyla oluşturuldu";
    public static string CouponUpdated = "Kupon güncellendi";
    public static string CouponDeleted = "Kupon silindi";
    public static string CouponAlreadyExists = "Bu kupon kodu zaten kullanılıyor";
    public static string CouponInactive = "Bu kupon aktif değil";

    // Inventory
    public static string StockInsufficient = "Stok yetersiz";
    public static string StockUpdated = "Stok güncellendi";
    public static string StockNotFound = "Stok kaydı bulunamadı";

    // CreditCard
    public static string CardAdded = "Kredi kartı eklendi";
    public static string CardDeleted = "Kredi kartı silindi";
    public static string CardNotFound = "Kredi kartı bulunamadı";
    public static string DefaultCardSet = "Varsayılan kart ayarlandı";

    // Address
    public static string AddressAdded = "Adres eklendi";
    public static string AddressDeleted = "Adres silindi";
    public static string AddressUpdated = "Adres güncellendi";
    public static string AddressNotFound = "Adres bulunamadı";
}
