# Backup / Restore Planı

Bu doküman PostgreSQL, Redis ve RabbitMQ için yedekleme ve geri yükleme adımlarını tanımlar.

## 1. Kapsam

| Bileşen | Volume | Container | Ne yedeklenir |
|---------|--------|-----------|---------------|
| PostgreSQL | `postgres_prod_data` | `ecommerce-db-prod` | Tüm veritabanı (şema + veri) |
| Redis | `redis_data` | `ecommerce-redis` | AOF/RDB snapshot (sepet, cache, lock state) |
| RabbitMQ | `rabbitmq_data` | `ecommerce-rabbitmq` | Definitions (exchange, queue, binding, user) |

Elasticsearch bu planda dahil değil. Arama indexi asıl veri kaynağı değil; PostgreSQL'den yeniden oluşturulabilir.

## 2. PostgreSQL

Ana veri kaynağı. Migration'lar EF Core Code-First ile yönetiliyor, veri sıfırdan seed ile oluşturulabiliyor; ama production verisi (siparişler, kullanıcılar, stok hareketleri) kaybedilmemeli.

### 2.1 Yedekleme

```bash
# Tam veritabanı yedeği (custom format, sıkıştırılmış)
docker exec ecommerce-db-prod pg_dump \
  -U "${DATABASE_PROD_USER}" \
  -d "${DATABASE_PROD_NAME}" \
  -Fc \
  -f /tmp/ecommerce_backup.dump

# Yedeği host'a kopyala
docker cp ecommerce-db-prod:/tmp/ecommerce_backup.dump ./backups/ecommerce_$(date +%Y%m%d_%H%M%S).dump
```

Plain SQL olarak almak isterseniz:

```bash
docker exec ecommerce-db-prod pg_dump \
  -U "${DATABASE_PROD_USER}" \
  -d "${DATABASE_PROD_NAME}" \
  --clean --if-exists \
  -f /tmp/ecommerce_backup.sql
```

### 2.2 Geri Yükleme

```bash
# Yedeği container'a kopyala
docker cp ./backups/ecommerce_YYYYMMDD_HHMMSS.dump ecommerce-db-prod:/tmp/restore.dump

# Geri yükle (mevcut veritabanını override eder)
docker exec ecommerce-db-prod pg_restore \
  -U "${DATABASE_PROD_USER}" \
  -d "${DATABASE_PROD_NAME}" \
  --clean --if-exists \
  /tmp/restore.dump
```

Geri yükleme sonrası API'yi restart edin; EF migration state'i veritabanındaki `__EFMigrationsHistory` tablosundan okunur.

### 2.3 Periyot ve Saklama

| Ortam | Sıklık | Saklama |
|-------|--------|---------|
| Production | Günlük (gece 03:00) | Son 7 gün |
| Staging | Deploy öncesi | Son 3 adet |

Otomatik yedekleme cron ile veya CI scheduled workflow ile yapılabilir.

### 2.4 Doğrulama

Yedek dosyasının sağlıklı olduğunu doğrulamak için:

```bash
pg_restore --list ./backups/ecommerce_YYYYMMDD_HHMMSS.dump > /dev/null
echo $?  # 0 ise dosya sağlam
```

## 3. Redis

Redis bu projede cache, sepet verisi ve distributed lock için kullanılıyor. Verilerin büyük çoğunluğu geçici; kaybolsa da sistem çalışmaya devam eder. Sepet verileri kullanıcı deneyimi açısından önemli ama kritik iş verisi değil.

### 3.1 Yedekleme

Redis varsayılan olarak RDB snapshot alır. Volume mount'u (`redis_data:/data`) sayesinde container restart'larında veri korunur.

Manuel snapshot almak için:

```bash
# Anlık RDB snapshot tetikle
docker exec ecommerce-redis redis-cli BGSAVE

# Snapshot'ın tamamlanmasını bekle
docker exec ecommerce-redis redis-cli LASTSAVE

# dump.rdb'yi host'a kopyala
docker cp ecommerce-redis:/data/dump.rdb ./backups/redis_$(date +%Y%m%d_%H%M%S).rdb
```

### 3.2 Geri Yükleme

```bash
# Redis'i durdur
docker compose stop redis

# RDB dosyasını volume'a kopyala
docker cp ./backups/redis_YYYYMMDD_HHMMSS.rdb ecommerce-redis:/data/dump.rdb

# Redis'i başlat
docker compose start redis
```

### 3.3 Kayıp Toleransı

Redis verisi kaybolursa:

- Cache kendini yeniden ısıtır (ilk isteklerde yavaşlama olabilir)
- Sepet verileri sıfırlanır (kullanıcı tekrar ürün ekler)
- Distributed lock'lar temizlenir (sorun olmaz, yeniden alınır)
- Rate limit counter'ları sıfırlanır (kısa süreli limit aşımı riski)

Kısacası Redis kaybı uygulama durdurmaz ama kullanıcı deneyimini geçici olarak etkiler.

## 4. RabbitMQ

RabbitMQ topology'si (exchange, queue, binding) MassTransit tarafından otomatik oluşturuluyor. API her başladığında eksik queue/exchange tanımları yeniden yaratılır. Bu nedenle RabbitMQ için tam veri yedeği gerekmez; definitions export yeterlidir.

### 4.1 Definitions Export

```bash
# Tüm topology'yi JSON olarak dışa aktar
docker exec ecommerce-rabbitmq rabbitmqctl export_definitions /tmp/rabbit_definitions.json

# Host'a kopyala
docker cp ecommerce-rabbitmq:/tmp/rabbit_definitions.json ./backups/rabbit_definitions_$(date +%Y%m%d_%H%M%S).json
```

### 4.2 Geri Yükleme

```bash
# Definitions dosyasını container'a kopyala
docker cp ./backups/rabbit_definitions_YYYYMMDD_HHMMSS.json ecommerce-rabbitmq:/tmp/definitions.json

# İçe aktar
docker exec ecommerce-rabbitmq rabbitmqctl import_definitions /tmp/definitions.json
```

### 4.3 Kayıp Toleransı

RabbitMQ volume kaybedilirse:

- Queue ve exchange tanımları API restart'ta MassTransit tarafından yeniden oluşturulur
- Kuyrukta bekleyen mesajlar kaybolur
- `_error` kuyruğundaki mesajlar kaybolur (bunları zaten incelemediyseniz sorun olmaz)

Outbox pattern sayesinde henüz publish edilmemiş event'ler PostgreSQL'de tutulur; API tekrar ayağa kalktığında outbox publisher bunları RabbitMQ'ya iletir.

## 5. Felaket Senaryosu

Tüm volume'lar kaybolursa minimum kurtarma adımları:

1. PostgreSQL yedeğini geri yükle (en kritik adım, veri kaybını önler)
2. Servisleri ayağa kaldır: `docker compose --profile prod up -d`
3. API başlarken migration + seed otomatik çalışır
4. MassTransit queue/exchange tanımlarını otomatik oluşturur
5. Outbox publisher PostgreSQL'deki bekleyen event'leri yeniden yayınlar
6. Elasticsearch indexi ilk ürün sync event'i veya manual reindex ile dolmaya başlar
7. `health/ready` ve smoke testleri çalıştırarak doğrula

```bash
curl -fsS http://localhost:5001/health/ready
API_BASE_URL=http://localhost:5001 SMOKE_EXPECT_SWAGGER=false ./scripts/ci/run_api_smoke.sh
```

## 6. Backups Klasörü

Yedek dosyalarını `./backups/` klasöründe saklayın. `.gitignore`'a eklemeyi unutmayın:

```bash
mkdir -p backups
echo "backups/" >> .gitignore
```
