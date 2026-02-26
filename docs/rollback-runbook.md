# Rollback Runbook

Bu doküman, production benzeri ortamda sorunlu bir deploy sonrası geri dönüş adımlarını standartlaştırmak için hazırlandı.

## 1. Kapsam

Rollback kapsamındaki ana bileşenler:

- API (`api-prod`)
- RabbitMQ (`order-created`, `product-index-sync`, `*_error` kuyrukları)
- Elasticsearch (arama indexi)
- PostgreSQL (migration uyumu)

## 2. Rollback Tetikleyicileri

Aşağıdaki durumlardan biri varsa rollback değerlendirilir:

- 5xx hata oranı hızlı şekilde artıyorsa
- `product-index-sync_error` veya `order-created_error` kuyrukları hızlı büyüyorsa
- Search endpoint sağlıklı yanıt veremiyorsa
- Yeni sürümde kritik iş akışlarında regresyon varsa (checkout, support, search)

## 3. Rollback Öncesi Hızlı Toplama

Rollback öncesi kanıt/log alın:

```bash
docker logs --tail=300 ecommerce-api-prod > /tmp/api-prod-pre-rollback.log
docker exec ecommerce-rabbitmq rabbitmqctl list_queues -p /ecommerce name messages consumers > /tmp/rabbit-queues-pre-rollback.txt
```

## 4. Uygulama Sürümü Rollback

Bu projede en güvenli geri dönüş: son stabil commit'e dönüp prod servisini yeniden build etmek.

```bash
# 1) Son stabil commit'e gec
git checkout <stable_commit_sha>

# 2) Prod API'yi yeniden ayağa kaldir
docker compose --profile prod up -d --build api-prod

# 3) API health kontrol
curl -i http://localhost:5001/swagger/index.html
```

Not:

- Migration rollback gerekmiyorsa sadece uygulama sürümü geri alın.
- Veri kaybına yol açabilecek manuel DB işlemlerinden kaçının.

## 5. RabbitMQ / Consumer Kontrolleri

Rollback sonrası kuyruk ve consumer durumunu doğrula:

```bash
docker exec ecommerce-rabbitmq rabbitmqctl list_queues -p /ecommerce name messages consumers
```

Beklenen:

- `product-index-sync` ve `order-created` için `consumers > 0`
- `_error` kuyruklarında hızlı artış olmaması

Hata kuyruğunu temizleme:

- İlk önce mesajı inceleyin (doğrudan purge önermeyin).
- Sorun kök neden çözülmeden `_error` kuyruğunu silmeyin.

## 6. Elasticsearch Fallback Doğrulaması

Search servisi kritik olduğu için rollback sonrası minimum smoke:

```bash
curl -s "http://localhost:5001/api/v1/search/products?q=adidas&page=1&pageSize=5" | jq '.success'
```

Beklenen: `true`

## 7. Release Gate Sonrası Kapanış

Rollback uygulandıktan sonra:

1. Build ve test gate çalıştır
2. API smoke test geçişlerini teyit et
3. Olay notunu kaydet

Örnek olay notu:

- Tarih/saat
- Tetikleyici (hangi alarm)
- Rollback alınan sürüm
- Doğrulanan kontroller
- Kalan aksiyonlar

## 8. Ek Not

`PRODUCT_INDEX_SYNC_FORCE_FAIL` değişkeni sadece test içindir. Production ortamında `false` kalmalıdır.
