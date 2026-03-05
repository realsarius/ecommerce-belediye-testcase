# Görsel Yükleme Sistemi Checklist (Cloudflare R2)

Tarih: 2026-03-05

## 1) Amaç

Bu planın amacı ürün, kategori ve satıcı profil görsellerini URL manuel girişi yerine güvenli ve ölçeklenebilir bir yükleme akışına taşımaktır

- [ ] Ürün görselleri için yükleme, sıralama, ana görsel seçimi ve silme akışlarını tamamla
- [x] Kategori görselleri için admin upload akışını tamamla
- [x] Satıcı profil logo ve banner görselleri için upload akışını tamamla
- [ ] Orphan dosya temizliği ve operasyonel bakım joblarını ekle

## 2) Teknoloji Kararı

Seçilen storage: Cloudflare R2

Gerekçe:

- [x] Mevcut Cloudflare altyapısı ile doğal uyum
- [x] S3 uyumluluğu sayesinde .NET entegrasyonu kolay
- [x] CDN ve cache davranışı yönetimi kolay
- [x] Sunucu diskine bağlı kalınmadığı için ölçekleme ve backup riski azalır

Alternatifler:

- [ ] Hetzner Object Storage ikinci opsiyon olarak dursun
- [ ] Sunucu diski sadece local/dev fallback olarak kalsın

## 3) Mimari Hedef

Hedef model: Presigned URL upload (backend üzerinden dosya stream etmeden)

- [x] Frontend backendden presign ister
- [x] Backend context bazlı object key üretir
- [x] Frontend dosyayı direkt R2ye PUT eder
- [x] Frontend confirm endpointine object key bildirir
- [x] Backend ownership doğrular, DByi günceller

Not:

- [ ] Return upload tarafındaki mevcut local-disk akışı korunur
- [ ] Ürün/kategori/satıcı görselleri için ayrı object storage servisi eklenir

## 4) Domain Model ve DB Değişiklikleri

Mevcut model gerçekleri:

- [x] `ProductImage` alanı `SortOrder` kullanıyor, `DisplayOrder` değil
- [x] Satıcı profilinde `LogoUrl` ve `BannerImageUrl` zaten var
- [x] `Category` görsel alanı henüz yok

Planlanan ek alanlar:

- [x] `TBL_ProductImages` için `ObjectKey` kolonu ekle
- [x] `TBL_Categories` için `ImageUrl` ve `ImageObjectKey` kolonları ekle
- [x] `TBL_SellerProfiles` için `LogoObjectKey` ve `BannerImageObjectKey` kolonları ekle

Migration:

- [x] Tek migration ile tüm storage key kolonlarını ekle
- [x] Snapshot ve configuration dosyalarını güncelle

## 5) Backend Planı (Clean Architecture Uyumlu)

Servisler:

- [x] `IObjectStorageService` ve `R2ObjectStorageService` oluştur
- [x] `R2Settings` (AccountId, AccessKeyId, SecretAccessKey, BucketName, PublicBaseUrl, PresignedUrlExpiry) ekle
- [x] DI kaydını Infrastructure katmanında yap

API kontratı:

- [x] `POST /api/v1/media/presign`
- [x] `POST /api/v1/media/confirm`
- [x] `DELETE /api/v1/media/{imageId}`
- [x] `PUT /api/v1/media/products/{productId}/images/reorder`

İş kuralları:

- [x] Context tabanlı akış kullan (`product`, `category`, `seller-logo`, `seller-banner`)
- [x] Seller sadece kendi ürününe upload yapabilsin
- [x] Category upload sadece admin rolü ile yapılabilsin
- [x] Seller profil yükleme sadece kendi hesabına yapılabilsin

Teknik doğrulamalar:

- [x] `ContentType` allow-list kontrolü
- [x] `FileSizeBytes` üst limit kontrolü (10 MB)
- [x] `HeadObject` ile confirm öncesi dosya varlık kontrolü
- [x] Object key prefix ownership kontrolü

## 6) Frontend Planı

Ürün formu:

- [x] URL text input yerine upload bileşeni koy
- [x] Çoklu görsel desteği (max 8)
- [x] Ana görsel seçme desteği
- [x] Drag and drop sıralama desteği (`@dnd-kit`)

Kategori formu:

- [x] Tekli görsel upload (admin)

Satıcı profil formu:

- [x] Logo upload (1:1)
- [x] Banner upload (geniş oran)

UX:

- [x] Her dosya için progress göster
- [ ] Başarısız upload için retry butonu göster
- [ ] Silme ve yeniden sıralamada optimistic UI veya kontrollü loading kullan

## 7) Güvenlik Checklist

- [ ] IDOR zinciri: `imageId -> productId -> sellerId -> JWT userId` kontrolü
- [ ] MIME spoofing için magic byte kontrolü ekle
- [ ] Presign URL süresi kısa tutulmalı (ör. 5 dk)
- [ ] Public URL path traversal riskleri engellenmeli
- [ ] Silme işleminde sahiplik doğrulaması zorunlu olmalı

## 8) Operasyon ve Bakım

- [ ] Hangfire job: orphan görselleri günlük temizle
- [ ] Soft orphan penceresi uygula (ör. 24 saatten eski orphanları sil)
- [ ] Storage hataları için merkezi log ve alert başlıkları ekle
- [ ] R2 erişim anahtarlarını `.env.prod` ve secret management ile yönet

## 9) Fazlara Bölünmüş Uygulama Planı

### Faz 1 — Altyapı ve DB

- [x] R2 settings + service + DI
- [x] Storage key generator
- [x] DB migration (object key kolonları)

### Faz 2 — Backend Endpointleri

- [x] Presign endpoint
- [x] Confirm endpoint
- [x] Delete endpoint
- [x] Reorder endpoint

### Faz 3 — Frontend Ürün Upload

- [x] ImageUploader bileşeni
- [x] ProductForm entegrasyonu
- [x] Sıralama ve ana görsel UX

### Faz 4 — Kategori ve Satıcı Profil

- [x] Category image upload entegrasyonu
- [x] Seller logo/banner upload entegrasyonu

### Faz 5 — Temizlik ve Hardening

- [ ] Orphan cleanup job
- [ ] Güvenlik testleri
- [ ] Entegrasyon testleri

## 10) Test Stratejisi

Backend:

- [ ] Presign yetki testleri
- [ ] Confirm ownership testleri
- [ ] Delete ownership ve primary fallback testleri
- [ ] Reorder transaction testleri

Frontend:

- [ ] Upload progress ve hata durum testleri
- [ ] Reorder davranış testleri
- [ ] Ana görsel atama testleri

E2E:

- [ ] Seller ürün oluşturma + görsel yükleme + sıralama
- [ ] Admin kategori görsel yükleme
- [ ] Seller profil logo/banner güncelleme

## 11) Definition of Done

- [ ] Ürün görselleri URL manuel girişe ihtiyaç duymadan yüklenebiliyor
- [x] Kategori ve satıcı profil görselleri aynı altyapı ile çalışıyor
- [ ] Tüm endpointlerde yetki ve sahiplik kontrolleri doğrulandı
- [ ] CI testleri ve smoke akışları yeşil
- [ ] Orphan cleanup job prod schedule ile aktif
