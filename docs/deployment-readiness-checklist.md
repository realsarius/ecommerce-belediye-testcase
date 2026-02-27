# Deployment Readiness Checklist

Bu doküman, production deploy öncesi ve sonrası minimum kontrolleri standart hale getirmek için hazırlandı.

## 1. Kapsam

Checklist şu alanları kapsar:

- ortam değişkenleri ve secret hazırlığı
- production bağımlılıklarının sağlık durumu
- deploy anındaki temel komut akışı
- post-deploy smoke doğrulamaları
- rollback kararı verilirse minimum geri dönüş kontrolleri

## 2. Release Gate

Deploy öncesi şu kapılar yeşil olmalı:

- hedef commit/tag net olarak seçildi
- çalışma dizininde beklenmeyen local değişiklik yok
- CI pipeline `restore + build + test + frontend build + smoke` adımlarını geçti
- migration etkisi gözden geçirildi; veri kırıcı değişiklik varsa ek onay alındı
- rollback için bir önceki stabil commit/image bilgisi not edildi

Yerel son kontrol için:

```bash
dotnet test EcommerceAPI.sln --no-build
```

## 3. Env ve Secret Kontrolü

Production deploy öncesi `.env` veya secret store içinde en az şu alanlar gözden geçirilmeli:

- `DATABASE_PROD_HOST`, `DATABASE_PROD_PORT`, `DATABASE_PROD_NAME`, `DATABASE_PROD_USER`, `DATABASE_PROD_PASSWORD`
- `JWT_SECRET_KEY`, `JWT_ISSUER`, `JWT_AUDIENCE`, `JWT_EXPIRATION_MINUTES`
- `ALLOWED_ORIGINS`, `ALLOWED_HOSTS`
- `REDIS_CONNECTION_STRING`
- `RABBITMQ_PORT`, `RABBITMQ_USER`, `RABBITMQ_PASSWORD`, `RABBITMQ_VHOST`, `RABBITMQ_PREFETCH_COUNT`
- `ELASTICSEARCH_URL`
- `ENCRYPTION_KEY`, `HASH_PEPPER`
- `IYZICO_API_KEY`, `IYZICO_SECRET_KEY`, `IYZICO_BASE_URL`
- `PRODUCT_INDEX_SYNC_FORCE_FAIL=false`

Ek notlar:

- `JWT_SECRET_KEY` en az 32 byte olmalı; uygulama fail-fast davranır.
- `ALLOWED_ORIGINS` içinde sadece gerçek production origin'leri kalmalı.
- `ALLOWED_HOSTS` wildcard olmamalı; production host listesi ile sınırlı tutulmalı.
- DataProtection key path/volume mount'i kalıcı olmalı.

## 4. Altyapı Hazırlığı

Deploy öncesi production profili syntax ve servis sağlığı doğrulanmalı:

```bash
docker compose --profile prod config >/dev/null
docker compose --profile prod ps
docker exec ecommerce-rabbitmq rabbitmq-diagnostics -q ping
docker exec ecommerce-db-prod pg_isready -U "${DATABASE_PROD_USER}" -d "${DATABASE_PROD_NAME}"
```

Beklenen:

- `postgres-prod`, `rabbitmq`, `api-prod` ayağa kalkabilecek durumda olmalı
- RabbitMQ vhost ve credentials geçerli olmalı
- PostgreSQL bağlantısı ve hedef veritabanı hazır olmalı
- kalıcı volume'ler mevcut olmalı

Elasticsearch ayakta ise ek kontrol:

```bash
curl -fsS http://localhost:9200/_cluster/health >/dev/null
```

## 5. Deploy Akışı

Tipik production deploy akışı:

```bash
docker compose --profile prod up -d --build api-prod
docker logs --tail=200 ecommerce-api-prod
```

Migration içeren bir release ise:

- deploy penceresi önceden duyurulmalı
- rollback alınacak stabil sürüm not edilmeli
- deploy sonrası ilk 10-15 dakika queue, error rate ve latency yakından izlenmeli

## 6. Post-Deploy Smoke

Production ortamında Swagger varsayılan olarak açık olmayabilir; bu yüzden smoke doğrulaması health ve ana iş akışlarına dayanmalı.

Minimum manuel kontroller:

```bash
curl -fsS http://localhost:5001/health/live >/dev/null
curl -fsS http://localhost:5001/health/ready >/dev/null
curl -fsS "http://localhost:5001/api/v1/search/products?q=test&page=1&pageSize=5" >/dev/null
curl -fsS "http://localhost:5001/api/v1/search/suggestions?q=ad&limit=5" >/dev/null
docker exec ecommerce-rabbitmq rabbitmqctl list_queues -p /ecommerce name messages consumers
```

Script ile smoke çalıştırmak için:

```bash
API_BASE_URL=http://localhost:5001 SMOKE_EXPECT_SWAGGER=false ./scripts/ci/run_api_smoke.sh
```

Beklenen:

- `health/live` ve `health/ready` 200 dönmeli
- search ve suggestion endpoint'leri başarılı dönmeli
- `product-index-sync` ve `order-created` için `consumers > 0` olmalı
- `_error` kuyruklarında anormal artış olmamalı

## 7. Rollback Doğrulaması

Rollback ihtiyacı doğarsa aşağıdaki minimum kontroller tekrar koşulmalı:

- bir önceki stabil sürüme dönüldüğü teyit edilmeli
- `health/ready` yeniden başarılı dönmeli
- search fallback ve temel smoke akışları tekrar geçmeli
- queue consumer sayıları toparlanmalı
- latency ve 5xx oranları normale dönmeli

Detaylı geri dönüş adımları için:

- [Rollback Runbook](./rollback-runbook.md)

## 8. Release Kaydı

Deploy tamamlandığında kısa bir release notu bırakılmalı:

- deploy tarihi ve saati
- deploy edilen commit/tag
- migration olup olmadığı
- koşulan smoke komutları
- rollback gerekip gerekmediği
- kalan takip aksiyonları
