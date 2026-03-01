# Wishlist Smoke / E2E Checklist

Bu checklist wishlist özelliğinin uçtan uca kritik senaryolarını hızlıca doğrulamak için hazırlanmıştır.

## Test Öncesi Hazırlık

- API, frontend, Redis, RabbitMQ, Elasticsearch ve Kibana ayakta olmalı
- Test kullanıcısı için geçerli giriş bilgisi hazır olmalı
- En az bir aktif, bir pasif ve bir stokta olmayan ürün bulunmalı
- En az bir ürün için fiyat düşüşü ve düşük stok senaryosu tetiklenebilir durumda olmalı

## 1. Guest Wishlist Akışı

1. Çıkış yapın.
2. Ana sayfada bir ürünü favoriye ekleyin.
3. Header üzerindeki favori sayısının arttığını doğrulayın.
4. `/wishlist` sayfasına gidin.
5. Bekleyen favorilerin listelendiğini doğrulayın.
6. Aynı ürünü tekrar kaldırın.
7. Sayaç ve liste durumunun güncellendiğini doğrulayın.

Beklenen sonuç:

- ürün local pending wishlist'e eklenir
- sayfa yenilense bile veri korunur
- kaldırma işlemi local state üzerinde anında çalışır

## 2. Login Sonrası Guest Sync

1. Guest durumda en az iki ürün favorileyin.
2. Giriş yapın.
3. Header ve wishlist sayfasını kontrol edin.
4. Pending ürünlerin gerçek wishlist'e senkronize edildiğini doğrulayın.

Beklenen sonuç:

- pending ürünler server wishlist'e taşınır
- local pending liste temizlenir
- duplicate ürün oluşmaz

## 3. Favoriye Ekle / Çıkar

1. Giriş yapmış kullanıcı ile ürün listeleme ekranına gidin.
2. Bir ürünü favoriye ekleyin.
3. Aynı ürünü tekrar kaldırın.
4. Ürün detay ekranından aynı akışı tekrarlayın.

Beklenen sonuç:

- toast mesajları doğru görünür
- heart state ürün kartı ve detay ekranında senkron kalır
- backend `401` üretmeden istekleri kabul eder

## 4. Token Refresh Dayanıklılığı

1. Giriş yapın.
2. Access token süresi dolana kadar bekleyin veya expired token ile test edin.
3. Ardından `/cart`, `/wishlists`, `/hubs/wishlist` kullanan akışları tetikleyin.

Beklenen sonuç:

- istemci otomatik refresh token akışını çalıştırır
- istekler bir kez daha denenir
- kullanıcı görünürde login kalırken korumalı akışlar çalışmaya devam eder
- refresh başarısızsa kullanıcı logout edilir

## 5. Koleksiyonlar / Çoklu Listeler

1. Wishlist sayfasında yeni koleksiyon oluşturun.
2. Bir ürünü yeni koleksiyona taşıyın.
3. Koleksiyon filtresiyle listeyi değiştirin.

Beklenen sonuç:

- koleksiyon oluşturma başarılı olur
- ürün yalnızca hedef koleksiyonda görünür
- filtrelenen liste doğru item setini gösterir

## 6. Paylaşılabilir Wishlist

1. Wishlist paylaşımını aktif edin.
2. Oluşan public linki kopyalayın.
3. Gizli sekmede linki açın.
4. Paylaşımı kapatıp aynı linki tekrar test edin.

Beklenen sonuç:

- public sayfa read-only çalışır
- ürünler ve koleksiyon bilgileri doğru görünür
- paylaşım kapatıldığında link artık erişilemez olur

## 7. Fiyat Alarmı

1. Wishlist sayfasından bir ürün için hedef fiyat tanımlayın.
2. İlgili ürün fiyatını hedefin altına düşürün.
3. Hangfire job veya ilgili akışı tetikleyin.

Beklenen sonuç:

- price alert kaydı oluşur
- event publish edilir
- SignalR üzerinden kullanıcıya bildirim gelir
- analytics log oluşur

## 8. Düşük Stok Bildirimi

1. Wishlist'te bulunan bir ürünün stok miktarını eşik altına düşürün.
2. İlgili stok güncelleme akışını çalıştırın.

Beklenen sonuç:

- `WishlistProductLowStockEvent` publish edilir
- kullanıcı toast / SignalR bildirimi alır
- aynı ürün için gereksiz tekrar bildirim üretilmez

## 9. Tümünü Sepete Ekle

1. Wishlist içinde aktif, pasif ve stok dışı ürünler bulundurun.
2. `Tümünü Sepete Ekle` aksiyonunu çalıştırın.

Beklenen sonuç:

- uygun ürünler sepete eklenir
- uygunsuz ürünler özet içinde ayrı raporlanır
- başarı ve atlanan ürün sayıları doğru görünür

## 10. Cursor Pagination

1. Wishlist'e 20'den fazla ürün ekleyin.
2. Wishlist sayfasını açın.
3. `Daha Fazla Yükle` akışını kullanın.

Beklenen sonuç:

- ilk yüklemede yalnızca ilk sayfa gelir
- sonraki sayfalar `nextCursor` ile eklenir
- duplicate item görünmez

## 11. Unavailable Ürün Durumu

1. Wishlist'te bulunan bir ürünü pasife alın.
2. Wishlist sayfasını yenileyin.

Beklenen sonuç:

- ürün sessizce kaybolmaz
- unavailable state ile görünür
- sepete ekleme aksiyonu engellenir

## 12. En Çok Favorilenenler Widget'ı

1. Farklı ürünlere farklı sayıda favori ekleyin.
2. Ana sayfayı açın.

Beklenen sonuç:

- widget yalnızca `wishlistCount > 0` ürünleri gösterir
- sıralama en yüksek favori sayısından başlar
- kart görsel dili ana sayfa temasına uyumlu görünür

## 13. Search / Cache Senkronu

1. Bir ürünü favoriye ekleyin veya çıkarın.
2. Ürün listeleme, detay ve arama ekranlarını yenileyin.

Beklenen sonuç:

- `wishlistCount` güncellenmiş görünür
- eski cache değeri kalmaz
- Elasticsearch index verisi backend ile tutarlı olur

## 14. Kibana Kontrolü

1. Kibana'da `Wishlist Analytics Logs` data view'ını açın.
2. Discover ekranında `fields.AnalyticsStream.keyword = Wishlist` filtresi uygulayın.

Beklenen sonuç:

- wishlist event logları listelenir
- event türleri ve kategori alanları dolu gelir
- dashboard panelleri için gerekli alanlar seçilebilir durumdadır

## Test Sonu Notları

Her smoke turu sonunda aşağıdakileri kısa not olarak kaydetmek önerilir:

- başarısız senaryo adı
- görülen HTTP durum kodu
- ilgili log / correlation id
- gerekiyorsa ekran görüntüsü veya video
