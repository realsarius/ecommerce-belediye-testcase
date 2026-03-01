# Wishlist Kibana Dashboard Kurulumu

Bu doküman wishlist analytics panellerini Kibana üzerinde adım adım manuel kurmak için hazırlanmıştır.

## Hazır Durum

- Elasticsearch log index deseni: `ecommerce-logs-*`
- Zaman alanı: `@timestamp`
- Wishlist analytics alanları doğrulandı:
  - `fields.AnalyticsStream.keyword`
  - `fields.AnalyticsEvent.keyword`
  - `fields.FunnelStage.keyword`
  - `fields.NotificationChannel.keyword`
  - `fields.ProductId`
  - `fields.WishlistCount`
  - `fields.TargetPrice`
  - `fields.StockQuantity`
  - `fields.OccurredAt`
  - `fields.Category.keyword`
  - `fields.MessageId.keyword`

## 1. Data View

Kibana `default` space içinde aşağıdaki data view kullanılmalıdır:

- Ad: `Wishlist Analytics Logs`
- Pattern: `ecommerce-logs-*`
- Time field: `@timestamp`

Local geliştirme ortamında bu data view oluşturuldu. Eğer görünmüyorsa Kibana'da yeniden şu bilgilerle oluşturun:

1. `Stack Management`
2. `Data Views`
3. `Create data view`
4. Yukarıdaki alanları girip kaydedin

## 2. Dashboard Oluşturma

Yeni dashboard adı:

- `Wishlist Analytics Overview`

Önerilen global filtre:

- `fields.AnalyticsStream.keyword is Wishlist`

Önerilen zaman aralığı:

- `Last 7 days`

## 3. Panel Seti

### Panel 1: Saatlik Favori Ekleme Trendi

- Tür: `Lens`
- Görselleştirme: `Line`
- X ekseni: `@timestamp`
- Interval: `Hourly`
- Y ekseni: `Count`
- Breakdown: `fields.Category.keyword`
- Filtre: `fields.AnalyticsEvent.keyword is WishlistItemAddedEvent`

### Panel 2: Favoriden Sepete Dönüşüm

- Tür: `Lens`
- Görselleştirme: `Bar stacked`
- X ekseni: `fields.AnalyticsEvent.keyword`
- Y ekseni: `Count`
- Filtre:
  - `fields.AnalyticsEvent.keyword is one of`
  - `WishlistItemAddedEvent`
  - `WishlistItemAddedToCart`
  - `WishlistBulkAddToCartCompleted`

### Panel 3: Fiyat Alarmı Etkinliği

- Tür: `Lens`
- Görselleştirme: `Bar`
- X ekseni: `fields.AnalyticsEvent.keyword`
- Y ekseni: `Count`
- Breakdown: `fields.Category.keyword`
- Filtre:
  - `fields.AnalyticsEvent.keyword is one of`
  - `WishlistPriceAlertTriggered`
  - `WishlistPriceAlertDelivered`

### Panel 4: Düşük Stok Bildirimleri

- Tür: `Lens`
- Görselleştirme: `Bar`
- X ekseni: `fields.AnalyticsEvent.keyword`
- Y ekseni: `Count`
- Breakdown: `fields.Category.keyword`
- Filtre:
  - `fields.AnalyticsEvent.keyword is one of`
  - `WishlistLowStockDelivered`
  - `WishlistLowStockSkipped`

### Panel 5: En Çok Favorilenen Kategoriler

- Tür: `Lens`
- Görselleştirme: `Horizontal bar`
- Y ekseni: `Top values of fields.Category.keyword`
- X ekseni: `Count`
- Filtre: `fields.AnalyticsEvent.keyword is WishlistItemAddedEvent`

### Panel 6: Toplu Sepete Ekle Başarı Özeti

- Tür: `Metric`
- Ana metrik: `Average of fields.AddedCount`
- İkinci metrik: `Average of fields.SkippedCount`
- Filtre: `fields.AnalyticsEvent.keyword is WishlistBulkAddToCartCompleted`

### Panel 7: Bildirim Kanalı Dağılımı

- Tür: `Donut`
- Dilim: `Top values of fields.NotificationChannel.keyword`
- Metrik: `Count`
- Filtre:
  - `fields.AnalyticsEvent.keyword is one of`
  - `WishlistPriceAlertDelivered`
  - `WishlistLowStockDelivered`

## 4. Kaydedilmiş Aramalar

Discover içinde aşağıdaki saved search'lerin oluşturulması önerilir:

### Wishlist Events

- Filtre: `fields.AnalyticsStream.keyword is Wishlist`
- Kolonlar:
  - `@timestamp`
  - `fields.AnalyticsEvent`
  - `fields.ProductId`
  - `fields.Category`
  - `fields.UserId`
  - `fields.OccurredAt`

### Wishlist Notifications

- Filtre:
  - `fields.AnalyticsEvent.keyword is one of`
  - `WishlistPriceAlertDelivered`
  - `WishlistLowStockDelivered`
- Kolonlar:
  - `@timestamp`
  - `fields.AnalyticsEvent`
  - `fields.NotificationChannel`
  - `fields.ProductId`
  - `fields.Category`
  - `fields.MessageId`

## 5. Kontrol Listesi

Dashboard tamamlandıktan sonra şu kontroller yapılmalıdır:

1. `fields.AnalyticsStream.keyword = Wishlist` filtresi veri dönüyor mu kontrol edin.
2. `WishlistItemAddedEvent` için son 7 günde en az bir kayıt var mı kontrol edin.
3. Fiyat alarmı ve düşük stok event'leri yoksa panellerin boş görünmesi normal; bu durumda paneli kaldırmayın.
4. `Count` ve `Top values` kullanan panellerde `.keyword` alanlarının seçildiğini doğrulayın.
5. Dashboard'u `default` space içinde kaydedin.

## 6. Sonraki Adım

Bu kurulum tamamlandıktan sonra bir sonraki operasyonel iş:

- wishlist smoke / E2E checklist hazırlamak
