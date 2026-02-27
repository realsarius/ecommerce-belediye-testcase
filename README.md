# E-Commerce API

## İçindekiler

- [0. Hızlı Kurulum](#0-hızlı-kurulum)
- [1. Kapsam](#1-kapsam)
- [2. Teknoloji Yığını](#2-teknoloji-yığını-technology-stack)
- [3. Veritabanı Tasarımı](#3-veritabanı-tasarımı-database-design)
- [4. API Tasarımı](#4-api-tasarımı-ve-standartlar-api-design)
- [5. Loglama ve Hata Yönetimi](#5-loglama-i̇zlenebilirlik-ve-hata-yönetimi-observability)
- [6. Test Stratejisi](#6-test-stratejisi-testing)
- [7. Kurulum ve Çalıştırma](#7-kurulum-ve-çalıştırma)
- [8. Frontend](#8-frontend)
- [9. Production Notes](#9-production-notes)

## 0. Hızlı kurulum

### Docker ile (Önerilen)

```bash
# 1) Ortam değişkenleri hazırlığı
cp .env.example .env

# 2) Development + Loglama ortamını başlat
docker compose --profile dev --profile logging up -d --build

# 3) Test ortamını başlat
docker compose --profile test up -d --build

# 4) Hepsini durdur
docker compose down

```

### Erişim Adresleri

- Frontend: <http://localhost:3000>
- API (Swagger): <http://localhost:5000/swagger>
- pgAdmin: <http://localhost:5050>
- Kibana: <http://localhost:5601>
- Elasticsearch: <http://localhost:9200>

---

## 1. Proje Kapsamı

**Kimlik Doğrulama ve Yetkilendirme**: JWT ve Refresh Token tabanlı kimlik doğrulama uyguladım. Access Token'lar kısa ömürlü; Refresh Token'lar veritabanında hash'lenmiş olarak saklanıyor. Rol bazlı yetkilendirme (RBAC) ile Customer, Seller, Support ve Admin yetkilerini endpoint seviyesinde ayrıştırdım.

**Ürün Kataloğu**: Listeleme, filtreleme ve sayfalama mevcut. Sık erişilen veriler için Redis Cache kullanılıyor; ürün ekleme/güncelleme işlemlerinde ilgili cache invalidate ediliyor.

**Admin Ürün Yönetimi**: Satıcılar ve Adminler için CRUD operasyonları. Ürün güncellemeleri loglanıyor.

**Sepet Yönetimi**: Sepet verileri performans için Redis üzerinde tutuluyor. Atomic artırma/azaltma işlemleri ile Hash veri yapısı kullanılarak veritabanı yükü minimize edildi.

**Checkout ve Sipariş**: Sipariş oluşturma transaction bütünlüğü içinde gerçekleşiyor. Sipariş anında stok kontrolü, kupon doğrulaması ve kargo hesaplaması yapılır; sipariş `PendingPayment` statüsünde oluşturulup ödeme adımına yönlendirilir.

**Ödeme Entegrasyonu**: Iyzico (Sandbox) entegre edildi. Idempotency Key ile tekrar edilen ödemeler engelleniyor.

**Stok Yönetimi ve Tutarlılık**: Eşzamanlı siparişlerde stok tutarlılığı için Redis Distributed Lock (ürün bazlı, `product:{id}`) kullandım. Aynı ürüne eşzamanlı gelen taleplerde race condition ve oversell önleniyor.

## 2. Teknoloji Yığını (Technology Stack)

| Kategori | Teknoloji / Kütüphane | Kullanım Amacı |
|---|---|---|
| **Core & Architecture** | .NET 8, Clean Architecture, RESTful API | Modülerlik, test edilebilirlik, katmanlı mimari
| **Frontend** | React, Redux Toolkit, Shadcn/ui, Zod, Tailwind CSS, Vite | SPA, state yönetimi |
| **Data Access** | Entity Framework Core 8, PostgreSQL 16 | ORM, Migration yönetimi |
| **Dependency Injection & AOP** | Autofac | Gelişmiş DI, Interception, Aspect-Oriented Programming (Log, Cache, Validation, Transaction) |
| **Validation** | FluentValidation | Nesne doğrulama ve iş kuralları (AOP entegreli) |
| **Caching & Performance** | Redis 7, Distributed Cache | Önbellekleme, sepet yönetimi ve distributed lock |
| **Search & Indexing** | Elasticsearch 8 | Ürün arama, typo tolerance, index senkronu |
| **Realtime Communication** | SignalR | Canlı destek mesajlaşması (customer/support/admin) |
| **Messaging & Event Bus** | RabbitMQ, MassTransit | Asenkron event akışı, retry ve dead-letter yönetimi |
| **Logging & Monitoring** | Serilog, Elasticsearch, Kibana | Yapılandırılmış loglama, merkezi log yönetimi |
| **Auth** | JWT, BCrypt | Token tabanlı kimlik doğrulama ve parola hash'leme |
| **DevOps** | Docker, Docker Compose | Konteynerizasyon ve çoklu servis orkestrasyonu |
| **Background Jobs** | Hangfire, PostgreSQL Storage | Zamanlanmış görevler, arka plan işlemleri |
| **Documentation** | Swagger / OpenAPI | API dokümantasyonu ve test arayüzü |
| **Testing** | xUnit, Moq, FluentAssertions | Birim testleri, mocking ve akıcı assertion |

## 3. Veritabanı Tasarımı (Database Design)

Veritabanı diyagramı Dbdiagram'da görselleştirildi:
> 🔗 **[Canlı Veritabanı Diyagramı (dbdiagram.io)](https://dbdiagram.io/d/694d9913b8f7d8688620ad62)**

### 3.1 Entity Listesi

1. **Users**: Sistem kullanıcıları.
2. **Roles**: Yetkilendirme rolleri (Customer, Seller, Support, Admin).
3. **SellerProfiles**: Satıcı profil bilgileri.
4. **Products**: Ürünler.
5. **Categories**: Ürün kategorileri.
6. **Inventories**: Ürün stok miktarları.
7. **InventoryMovements**: Stok değişim hareketleri (audit log).
8. **Orders**: Sipariş başlık bilgileri.
9. **OrderItems**: Sipariş kalemleri.
10. **Payments**: Ödeme işlemleri ve sonuçları.
11. **ShippingAddresses**: Teslimat adresleri.
12. **Carts**: Kullanıcı sepetleri.
13. **CartItems**: Sepet ürünleri.
14. **Coupons**: İndirim kodları.
15. **CreditCards**: Şifrelenmiş kart bilgileri.
16. **RefreshTokens**: Oturum yenileme anahtarları.
17. **SupportConversations**: Canlı destek konuşma başlıkları.
18. **SupportMessages**: Canlı destek mesaj kayıtları.

### 3.2 Migration ve Şema Yönetimi

**Entity Framework Core Code-First** metodolojisi kullanıldı.

- Değişiklikler kod tarafında (Entities) yapılır.
- `dotnet ef migrations add [MigrationName]` ile versiyonlu migration oluşturulur.
- Veritabanı her ortamda kod ile senkronize kalır.

## 4. API Tasarımı ve Standartlar (API Design)

Tutarlılık, öngörülebilirlik ve izlenebilirlik ön planda tutuldu.

### 4.1 Endpoint Standartları

Tüm endpoint'ler RESTful prensiplerine uygun ve versiyonlama stratejisi benimsenmiş durumda.

- **Base URL:** `/api/v1/{resource}` (Örn: `/api/v1/products`, `/api/v1/orders`)
- **HTTP Metotları:** GET, POST, PUT, DELETE, PATCH standartlara uygun.

### 4.2 Response ve Hata Modeli

Tüm cevaplar standart yapıda; frontend entegrasyonu kolaylaştırıldı.

**Başarılı Cevaplar (Success):**

```json
{
  "success": true,
  "message": "İşlem başarılı",
  "data": { }
}
```

**Hata Cevapları (Error):**
Tüm hatalar merkezi bir Middleware tarafından yakalanıp tek tipte döndürülüyor.

```json
{
  "traceId": "0HLQ8...",
  "errorCode": "INSUFFICIENT_STOCK",
  "message": "Talep edilen stok miktarı mevcut değil.",
  "details": {
    "productId": 123,
    "requested": 5,
    "available": 2
  }
}
```

### 4.3 Pagination (Sayfalama)

Liste dönen endpoint'lerde sayfalama standarttır.

- **Request:** `?page=1&pageSize=10`
- **Response Metadata:**

    ```json
    {
      "items": [],
      "page": 1,
      "pageSize": 10,
      "totalCount": 150,
      "totalPages": 15,
      "hasPreviousPage": false,
      "hasNextPage": true
    }
    ```

### 4.4 Swagger & OpenAPI

Tüm endpoint'ler Swagger UI üzerinden interaktif olarak test edilebilir. JWT Auth entegrasyonu mevcut.

### 4.5 Elasticsearch Ürün Arama

Ürün araması Elasticsearch üzerinden çalışır. Endpoint:

- `GET /api/v1/search/products?q=&categoryId=&minPrice=&maxPrice=&page=&pageSize=`

Özellikler:

- Sayfalama ve filtre desteği
- Kısmi eşleşme (prefix) ve typo toleransı (fuzzy)  
  Örnek: `q=adida` sorgusu `Adidas` ürünlerini döndürebilir
- Ürün `create/update/delete` sonrası index senkronu
- Elasticsearch erişilemezse DB fallback araması

Örnek istek:

```bash
curl "http://localhost:5000/api/v1/search/products?q=adida&page=1&pageSize=10"
```

## 5. Loglama, İzlenebilirlik ve Hata Yönetimi (Observability)

### 5.1 Structured Logging (Serilog + Elasticsearch)

**Serilog** ile yapılandırılmış loglama kuruldu. Loglar JSON formatında. Elasticsearch + Kibana entegrasyonu ile merkezi log yönetimi sağlandı.

### 5.2 Correlation ID / Trace ID (İzlenebilirlik)

- Her HTTP isteğine benzersiz bir `X-Correlation-Id` atanıyor.
- Bu ID, Serilog LogContext'e enjekte edilerek o istek süresince tüm loglara damgalanıyor.
- Response header'larına da eklenerek istemci tarafından takip edilebilir.

### 5.3 Global Exception Handler

Tüm hata yönetimi merkezi `ExceptionHandlingMiddleware` içinde:

- Farklı exception tipleri (`NotFoundException`, `InsufficientStockException`, `ValidationException`, `BusinessException`) yakalanıp uygun HTTP Status Code ve yapılandırılmış error body döndürülüyor.
- `traceId` ile hatanın izlenebileceği Correlation ID iletiliyor.
- Beklenmedik hatalar loglanıp istemciye hassas bilgi sızdırmayan genel mesaj döndürülüyor.

### 5.4 Audit Log (Kritik İş Akışları)

İş süreci izlenebilirliği için Stok Değişimleri gibi işlemler audit log ile kaydediliyor.

## 6. Test Stratejisi (Testing)

Kodun güvenilirliğini ve iş kurallarının doğruluğunu garanti altına almak için kapsamlı testler yazıldı.

### 6.1 Unit Testler

xUnit, Moq ve FluentAssertions kullanıldı.

### 6.2 Integration Testler

**WebApplicationFactory** altyapısı ile e2e testleri yazıldı. In-memory veritabanı ve test container'ları kullanıldı.

### 6.3 Test Komutları

```bash
# Tüm testleri çalıştır
dotnet test

# Sadece Unit testleri çalıştır
dotnet test --filter "FullyQualifiedName~UnitTests"

# Sadece Integration testleri çalıştır
dotnet test --filter "FullyQualifiedName~IntegrationTests"
```

## 7. Kurulum ve Çalıştırma

### 7.1 Gereksinimler

[Docker & Docker Compose](https://docs.docker.com/compose/)

### 7.2 Environment Değişkenleri

Ortam değişkenleri `.env.example` dosyasında tanımlı.

```bash
cp .env.example .env
```

`.env` dosyasını düzenleyerek aşağıdaki değerleri doldurun:

```bash
# Database
DATABASE_DEV_NAME=ecommerce_dev
DATABASE_DEV_USER=postgres
DATABASE_DEV_PASSWORD=yourpassword
DATABASE_DEV_PORT=5432

# JWT Auth
JWT_SECRET_KEY=
JWT_ISSUER=
JWT_AUDIENCE=
JWT_EXPIRATION_MINUTES=

# Iyzico Payment (Sandbox)
IYZICO_API_KEY=sandbox-xxx
IYZICO_SECRET_KEY=sandbox-xxx
IYZICO_BASE_URL=https://sandbox-api.iyzipay.com

# Security
ENCRYPTION_KEY=
HASH_PEPPER=

# Redis
REDIS_CONNECTION_STRING=localhost:6379
```

### 7.3 Docker Compose ile Çalıştırma (Önerilen)

Tüm servisleri (API, PostgreSQL, Redis, Frontend) tek komutla başlatabilirsiniz:

```bash
# Development ortamını başlat
docker compose --profile dev --profile logging up -d

# Test ortamını başlat
docker compose --profile test up -d

# Tüm servisleri durdur
docker compose --profile dev --profile test --profile logging down
```

**Servis Erişim Adresleri (Dev):**

| Servis | Port | URL |
|--------|------|-----|
| Frontend | 3000 | <http://localhost:3000> |
| API (Swagger) | 5000 | <http://localhost:5000/swagger> |
| pgAdmin | 5050 | <http://localhost:5050> |
| PostgreSQL | 5432 | - |
| Redis | 6379 | - |
| RabbitMQ AMQP | 5672 | - |
| RabbitMQ Management | 15672 | <http://localhost:15672> |
| Kibana (logging profili) | 5601 | <http://localhost:5601> |
| Elasticsearch (logging profili) | 9200 | <http://localhost:9200> |
| Hangfire Dashboard | 5000 | <http://localhost:5000/hangfire> |

### 7.4 Manuel Kurulum

**Gereksinimler:** .NET 8 SDK, PostgreSQL 16, Redis 7, Node.js 22 (LTS önerilir)

```bash
# 1. Bağımlılıkları yükle
dotnet restore

# 2. Veritabanı migration'larını uygula
dotnet ef database update --project EcommerceAPI.DataAccess --startup-project EcommerceAPI.API

# 3. API'yi çalıştır
cd EcommerceAPI.API
dotnet run

# 4. Frontend'i çalıştır
cd frontend
npm install
npm run dev
```

### 7.5 Seed Data (Örnek Veriler)

Uygulama **Development** modunda başlatıldığında, `EcommerceAPI.Seeder` katmanı [seed-data/](seed-data) klasöründeki JSON dosyalarını okuyarak veritabanına yükler.

JSON dosyaları: 10 kategori, 100+ ürün, stok kayıtları ([seed-data/](seed-data))

Kod ile oluşturulan: 4 rol, 4 test kullanıcısı ([SeedRunner](EcommerceAPI.Seeder/SeedRunner.cs))

**Kullanıcı Şifreleri**

| Email | Şifre |
|-------|-------|
| `testadmin@test.com` | `Test123!` |
| `testseller@test.com` | `Test123!` |
| `customer@test.com` | `Test123!` |
| `support@test.com` | `Test123!` |

### 7.6 Ödeme Sağlayıcısı (Iyzico Sandbox)

Projede Iyzico Sandbox entegrasyonu yapılmıştır. Gerçek para akışı bulunmaz.

Test kartları için: [iyzico/iyzipay-dotnet](https://github.com/iyzico/iyzipay-dotnet)

Iyzico Docs: <https://docs.iyzico.com/on-hazirliklar/sandbox>

### 7.7 Örnek Kullanım Akışı (cURL)

Aşağıda tam bir e-ticaret akışını gösteren cURL komutları:

```bash
# 0. Kullanıcı Kaydı (Register)
curl -X POST "http://localhost:5000/api/v1/auth/register" \
  -H "Content-Type: application/json" \
  -d '{"email":"demo@test.com","password":"Demo123!","firstName":"Demo","lastName":"User"}'

# 1. Kullanıcı Girişi (Login) → Token alın
curl -X POST "http://localhost:5000/api/v1/auth/login" \
  -H "Content-Type: application/json" \
  -d '{"email":"demo@test.com","password":"Demo123!"}'
# Response'dan "token" değerini kopyalayın

# 2. Ürünleri Listele
curl "http://localhost:5000/api/v1/products?page=1&pageSize=10"

# 3. Sepete Ürün Ekle (Token gerekli)
curl -X POST "http://localhost:5000/api/v1/cart/items" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer <TOKEN>" \
  -d '{"productId":103,"quantity":1}'

# 4. Checkout - Sipariş Oluştur (Token gerekli)
curl -X POST "http://localhost:5000/api/v1/orders" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer <TOKEN>" \
  -d '{"shippingAddress":"Örnek Mahalle, Test Sokak No:1, İstanbul","paymentMethod":"CreditCard"}'
# Response'dan "orderId" değerini alın

# 5. Ödeme Yap (Token gerekli)
curl -X POST "http://localhost:5000/api/v1/payments" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer <TOKEN>" \
  -d '{"orderId":<ORDER_ID>,"cardNumber":"5406670000000009","cardHolderName":"Demo User","expiryDate":"12/26","cvv":"123"}'

# 6. Siparişleri Listele (Token gerekli)
curl "http://localhost:5000/api/v1/orders" \
  -H "Authorization: Bearer <TOKEN>"
```

### 7.8 Swagger UI

API dokümantasyonu: `http://localhost:5000/swagger`

### 7.9 Postman Collection

Postman collection dosyası: `postman/EcommerceAPI.postman_collection.json`

**Collection İçeriği:**

| Klasör | Endpoint Sayısı | Açıklama |
|--------|-----------------|----------|
| Auth | 5 | Register, Login, Refresh, Revoke, Me |
| Products (Public) | 2 | Ürün listeleme ve detay |
| Categories (Public) | 2 | Kategori listeleme |
| Cart | 5 | Sepet işlemleri |
| Orders | 5 | Sipariş işlemleri |
| Payments | 3 | Ödeme (farklı test kartları) |
| Shipping Addresses | 4 | Adres yönetimi |
| Credit Cards | 4 | Kayıtlı kart işlemleri |
| Coupons | 5 | Kupon işlemleri |
| Seller Profile | 4 | Satıcı profil yönetimi |
| Admin - Products | 5 | Admin/Seller ürün yönetimi |
| Admin - Categories | 4 | Admin kategori yönetimi |
| Admin - Orders | 2 | Admin sipariş yönetimi |
| Admin - Sellers | 1 | Admin satıcı görüntüleme |
| Search | 1 | Elasticsearch ürün arama |
| Support (Live Chat) | 7 | Canlı destek konuşma/mesaj endpointleri |
| Full E-Commerce Flow | 8 | Uçtan uca test senaryosu |

## 8. Frontend

React 19 + TypeScript tabanlı SPA. Klasör yapısı:

```
frontend/src/
├── components/    # UI bileşenleri (Shadcn/ui)
├── features/      # Redux slices (auth, cart, orders, products...)
├── pages/         # Sayfa bileşenleri
├── hooks/         # Custom React hooks
└── types/         # TypeScript tipleri
```

**Sayfalar:** Home, Login, Register, Cart, Checkout, Orders, ProductDetail, Account, Addresses, CreditCards

**Admin Panel:** Ürün/Kategori/Sipariş yönetimi

**Seller Panel:** Ürün ekleme/düzenleme, stok güncelleme

## 9. Production Notes

### SignalR Ölçek Stratejisi

Canlı destek modülü tek instance çalışmada doğrudan SignalR ile çalışır.
Birden fazla API instance veya replica çalıştırılacaksa Redis backplane aktif edilmelidir.

Konfigürasyon:

- `SIGNALR_REDIS_BACKPLANE_ENABLED=true`
- `SIGNALR_CHANNEL_PREFIX=ecommerce-prod`
- `REDIS_CONNECTION_STRING=redis:6379`

Tercih gerekçesi:

- Redis zaten cache, distributed lock ve rate limiting için sistemde mevcut
- Bu nedenle ilk ölçekleme adımı için en düşük operasyonel maliyetli çözüm Redis backplane'dir
- Managed SignalR servisi daha sonra ihtiyaç halinde değerlendirilebilir

Operasyon notları:

- Tüm API instance'ları aynı Redis'e bağlanmalıdır
- Ortamlar arasında channel prefix ayrılmalıdır (`ecommerce-dev`, `ecommerce-prod`)
- Çoklu replica senaryosunda reverse proxy ve WebSocket timeout ayarları ayrıca gözden geçirilmelidir

### 9.1 RabbitMQ + Elasticsearch Senkron Akışı

- Ürün `create/update/delete/stock` işlemlerinde arama index senkronu doğrudan çağrı yerine event publish ile çalışır.
- API, `product-index-sync` kuyruğuna event bırakır; consumer Elasticsearch tarafında `Upsert/Delete` uygular.
- Retry (3 deneme, 2 saniye aralık) sonrası başarısız mesajlar `_error` kuyruğuna düşer.

### 9.2 Operasyonel Kontroller

Temel sağlık kontrolleri:

```bash
curl -fsS http://localhost:5000/health/ready >/dev/null
curl -fsS "http://localhost:5000/api/v1/search/products?q=test&page=1&pageSize=5" >/dev/null
curl -fsS "http://localhost:5000/api/v1/search/suggestions?q=ad&limit=5" >/dev/null
docker exec ecommerce-rabbitmq rabbitmqctl list_queues -p /ecommerce name messages consumers
```

Beklenen:

- `health/ready`'nin başarıyla dönmesi
- arama ve suggestion endpoint'lerinin 200 dönmesi
- `product-index-sync` kuyruğunda `consumers > 0`
- `_error` kuyruklarında sürekli artış olmaması

Development veya staging ortaminda Swagger dogrulamasi icin:

```bash
curl -fsS http://localhost:5000/swagger/index.html >/dev/null
```

Uygulama ayağa kalktıktan sonra hızlı doğrulama için doğrudan smoke script'i de çalıştırabilirsiniz:

```bash
./scripts/ci/run_api_smoke.sh
```

Production smoke icin:

```bash
API_BASE_URL=http://localhost:5001 SMOKE_EXPECT_SWAGGER=false ./scripts/ci/run_api_smoke.sh
```

### 9.3 Kritik Ortam Değişkenleri

- `RABBITMQ_HOST`, `RABBITMQ_PORT`, `RABBITMQ_VHOST`
- `RABBITMQ_USER`, `RABBITMQ_PASSWORD`
- `ELASTICSEARCH_URL`
- `JWT_SECRET_KEY`

Test amaçlı kontrollü hata üretimi için:

- `PRODUCT_INDEX_SYNC_FORCE_FAIL` (Production'da `false` kalmalı)

### 9.4 Deployment Readiness Checklist

Deploy oncesi env/secrets, post-deploy smoke ve rollback dogrulama adimlari icin:

- [`docs/deployment-readiness-checklist.md`](docs/deployment-readiness-checklist.md)

### 9.5 Rollback Runbook

Operasyon sırasında en sık bakılacak referanslar:

- [Operasyonel Kontroller](#92-operasyonel-kontroller)
- [`docs/deployment-readiness-checklist.md`](docs/deployment-readiness-checklist.md)
- [`scripts/ci/run_api_smoke.sh`](scripts/ci/run_api_smoke.sh)
- [`scripts/ci/run_api_perf_smoke.sh`](scripts/ci/run_api_perf_smoke.sh)
- [Observability ve Loglama](#5-loglama-i̇zlenebilirlik-ve-hata-yönetimi-observability)
- [`observability/prometheus-alerts.yml`](observability/prometheus-alerts.yml)
- [CI/CD Pipeline](.github/workflows/main.yml)

Pratik rollback yaklaşımı:

1. Son deploy sonrası regresyon varsa önce etkilenen alanı netleştirin (`search`, `support`, `checkout`, `payment`).
2. Mevcut release'in health ve smoke durumunu doğrulayın.
3. Sorun son değişiklikten geldiyse bir önceki stabil image/tag'e dönün.
4. Rollback sonrası `health/ready`, temel smoke ve queue kontrollerini tekrar çalıştırın.
5. `_error` kuyruğu, latency ve log akışı normale dönmeden rollback'i tamamlanmış saymayın.

### 9.6 Incident Response (Kısa Akış)

1. **Önce alanı ayırın**: Sorun `search`, `support`, `checkout`, `payment` veya genel API erişimi mi, önce bunu netleştirin.
2. **Temel sağlığı doğrulayın**: `health/ready`, gerekiyorsa Swagger (development/staging) ve temel smoke çağrılarını çalıştırın.
3. **Bağımlılıkları kontrol edin**:
   - `search` tarafında Elasticsearch ve `product-index-sync`
   - `support` tarafında SignalR/RabbitMQ/Redis
   - `checkout` tarafında PostgreSQL, Redis ve payment akışı
4. **Etkisini sınırlandırın**: Sorun consumer, dış servis veya son deploy kaynaklıysa gerekirse ilgili akışı geçici olarak daraltın ya da rollback'e gidin.
5. **Düzeltme sonrası yeniden doğrulayın**: Sadece endpoint'in 200 dönmesi yetmez; smoke, queue ve alert tarafı da normale dönmeli.
6. **Kapatırken not bırakın**: Kısa bir incident özeti çıkarın. Kök neden, etki süresi ve tekrarını önleyecek aksiyon backlog'a yazılsın.

Önerilen hızlı kontrol komutları:

```bash
curl -fsS http://localhost:5000/health/ready
curl -fsS "http://localhost:5000/api/v1/search/products?q=test&page=1&pageSize=5"
curl -fsS "http://localhost:5000/api/v1/search/suggestions?q=ad&limit=5"
docker exec ecommerce-rabbitmq rabbitmqctl list_queues -p /ecommerce name messages consumers
./scripts/ci/run_api_smoke.sh
```
